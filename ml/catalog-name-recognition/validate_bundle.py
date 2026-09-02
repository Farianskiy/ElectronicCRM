from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import uuid
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any


EXPECTED_BUNDLE_FORMAT_VERSION = "1.0"
EXPECTED_DATASET_FORMAT_VERSION = "1.0"
EXPECTED_SPLIT_ALGORITHM_VERSION = "sha256-normalized-product-name-v1"
MINIMUM_RECOMMENDED_PRODUCT_GROUP_COUNT = 30

EXPECTED_FILES = {
    "manifest.json",
    "train.jsonl",
    "validation.jsonl",
    "test.jsonl",
}

EXPECTED_SPLITS = (
    ("Train", "train.jsonl"),
    ("Validation", "validation.jsonl"),
    ("Test", "test.jsonl"),
)

SHA256_PATTERN = re.compile(r"^[0-9A-Fa-f]{64}$")

REQUIRED_RECORD_FIELDS = {
    "formatVersion",
    "feedbackId",
    "productName",
    "normalizedProductName",
    "productTypeId",
    "productTypeCode",
    "characteristicDefinitionId",
    "characteristicCode",
    "exampleKind",
    "feedbackType",
    "labelQuality",
    "suggestedRawValue",
    "suggestedNormalizedValue",
    "suggestedConfidence",
    "suggestedSource",
    "spanStart",
    "spanLength",
    "spanEnd",
    "finalNormalizedValue",
    "hasSuggestedSpan",
    "hasVerifiedAnswerSpan",
    "dictionaryTermId",
    "recognitionProfileId",
    "modelVersion",
    "importBatchId",
    "importRowId",
    "reviewerRole",
    "createdAtUtc",
    "finalizedAtUtc",
}


class BundleValidationError(Exception):
    pass


@dataclass(frozen=True)
class SplitValidationResult:
    split: str
    file_name: str
    example_count: int
    product_groups: frozenset[str]
    sha256: str


@dataclass(frozen=True)
class BundleValidationResult:
    example_count: int
    product_group_count: int
    split_results: tuple[SplitValidationResult, ...]
    dataset_sha256: str
    ready_for_training: bool
    readiness_blockers: tuple[str, ...]


def require_object(value: Any, location: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise BundleValidationError(f"{location}: ожидался JSON-объект.")

    return value


def require_array(value: Any, location: str) -> list[Any]:
    if not isinstance(value, list):
        raise BundleValidationError(f"{location}: ожидался JSON-массив.")

    return value


def require_string(value: Any, location: str, allow_empty: bool = False) -> str:
    if not isinstance(value, str):
        raise BundleValidationError(f"{location}: ожидалась строка.")

    if not allow_empty and not value.strip():
        raise BundleValidationError(f"{location}: строка не должна быть пустой.")

    return value


def require_bool(value: Any, location: str) -> bool:
    if not isinstance(value, bool):
        raise BundleValidationError(f"{location}: ожидалось логическое значение.")

    return value


def require_non_negative_int(value: Any, location: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise BundleValidationError(f"{location}: ожидалось целое число.")

    if value < 0:
        raise BundleValidationError(f"{location}: число не должно быть отрицательным.")

    return value


def require_sha256(value: Any, location: str) -> str:
    text = require_string(value, location)

    if SHA256_PATTERN.fullmatch(text) is None:
        raise BundleValidationError(
            f"{location}: ожидалась SHA-256 строка из 64 шестнадцатеричных символов."
        )

    return text.upper()


def require_uuid_string(value: Any, location: str, allow_none: bool = False) -> str | None:
    if value is None and allow_none:
        return None

    text = require_string(value, location)

    try:
        uuid.UUID(text)
    except ValueError as error:
        raise BundleValidationError(f"{location}: значение не является UUID.") from error

    return text


def require_optional_string(value: Any, location: str) -> str | None:
    if value is None:
        return None

    return require_string(value, location, allow_empty=True)


def require_optional_int(value: Any, location: str) -> int | None:
    if value is None:
        return None

    return require_non_negative_int(value, location)


def calculate_sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def calculate_expected_split(normalized_product_name: str) -> str:
    hash_input = (
        f"{EXPECTED_SPLIT_ALGORITHM_VERSION}\n{normalized_product_name}"
    ).encode("utf-8")
    hash_bytes = hashlib.sha256(hash_input).digest()
    hash_prefix = int.from_bytes(hash_bytes[:4], byteorder="big", signed=False)
    bucket = hash_prefix % 100

    if bucket < 80:
        return "Train"

    if bucket < 90:
        return "Validation"

    return "Test"


def parse_manifest(raw_manifest: bytes) -> dict[str, Any]:
    try:
        manifest_text = raw_manifest.decode("utf-8")
    except UnicodeDecodeError as error:
        raise BundleValidationError("manifest.json: файл не является UTF-8.") from error

    try:
        manifest_value = json.loads(manifest_text)
    except json.JSONDecodeError as error:
        raise BundleValidationError(
            f"manifest.json: некорректный JSON: строка {error.lineno}, "
            f"столбец {error.colno}: {error.msg}"
        ) from error

    return require_object(manifest_value, "manifest.json")


def validate_record(
    value: Any,
    split: str,
    file_name: str,
    line_number: int,
    feedback_ids: set[str],
) -> str:
    location = f"{file_name}, строка {line_number}"
    record = require_object(value, location)

    missing_fields = sorted(REQUIRED_RECORD_FIELDS - record.keys())

    if missing_fields:
        raise BundleValidationError(
            f"{location}: отсутствуют обязательные поля: {', '.join(missing_fields)}."
        )

    format_version = require_string(
        record["formatVersion"],
        f"{location}.formatVersion",
    )

    if format_version != EXPECTED_DATASET_FORMAT_VERSION:
        raise BundleValidationError(
            f"{location}.formatVersion: ожидалась версия "
            f"{EXPECTED_DATASET_FORMAT_VERSION}, получена {format_version}."
        )

    feedback_id = require_uuid_string(
        record["feedbackId"],
        f"{location}.feedbackId",
    )

    if feedback_id in feedback_ids:
        raise BundleValidationError(
            f"{location}.feedbackId: feedback {feedback_id} встречается повторно."
        )

    feedback_ids.add(feedback_id)

    require_string(record["productName"], f"{location}.productName")

    normalized_product_name = require_string(
        record["normalizedProductName"],
        f"{location}.normalizedProductName",
    )

    if normalized_product_name != normalized_product_name.strip():
        raise BundleValidationError(
            f"{location}.normalizedProductName: значение содержит пробелы "
            "в начале или конце."
        )

    require_uuid_string(record["productTypeId"], f"{location}.productTypeId")
    require_string(record["productTypeCode"], f"{location}.productTypeCode")
    require_uuid_string(
        record["characteristicDefinitionId"],
        f"{location}.characteristicDefinitionId",
    )
    require_string(
        record["characteristicCode"],
        f"{location}.characteristicCode",
    )

    example_kind = require_string(
        record["exampleKind"],
        f"{location}.exampleKind",
    )

    require_string(record["feedbackType"], f"{location}.feedbackType")
    require_string(record["labelQuality"], f"{location}.labelQuality")
    require_optional_string(
        record["suggestedRawValue"],
        f"{location}.suggestedRawValue",
    )
    require_optional_string(
        record["suggestedNormalizedValue"],
        f"{location}.suggestedNormalizedValue",
    )
    require_optional_string(
        record["suggestedSource"],
        f"{location}.suggestedSource",
    )
    require_optional_string(
        record["finalNormalizedValue"],
        f"{location}.finalNormalizedValue",
    )
    require_uuid_string(
        record["dictionaryTermId"],
        f"{location}.dictionaryTermId",
        allow_none=True,
    )
    require_uuid_string(
        record["recognitionProfileId"],
        f"{location}.recognitionProfileId",
        allow_none=True,
    )
    require_optional_string(record["modelVersion"], f"{location}.modelVersion")
    require_uuid_string(
        record["importBatchId"],
        f"{location}.importBatchId",
        allow_none=True,
    )
    require_uuid_string(
        record["importRowId"],
        f"{location}.importRowId",
        allow_none=True,
    )
    require_optional_string(record["reviewerRole"], f"{location}.reviewerRole")
    require_string(record["createdAtUtc"], f"{location}.createdAtUtc")
    require_string(record["finalizedAtUtc"], f"{location}.finalizedAtUtc")

    suggested_confidence = record["suggestedConfidence"]

    if suggested_confidence is not None:
        if isinstance(suggested_confidence, bool) or not isinstance(
            suggested_confidence,
            (int, float),
        ):
            raise BundleValidationError(
                f"{location}.suggestedConfidence: ожидалось число или null."
            )

        if suggested_confidence < 0 or suggested_confidence > 1:
            raise BundleValidationError(
                f"{location}.suggestedConfidence: значение должно находиться "
                "между 0 и 1."
            )

    span_start = require_optional_int(record["spanStart"], f"{location}.spanStart")
    span_length = require_optional_int(
        record["spanLength"],
        f"{location}.spanLength",
    )
    span_end = require_optional_int(record["spanEnd"], f"{location}.spanEnd")

    has_suggested_span = require_bool(
        record["hasSuggestedSpan"],
        f"{location}.hasSuggestedSpan",
    )
    has_verified_answer_span = require_bool(
        record["hasVerifiedAnswerSpan"],
        f"{location}.hasVerifiedAnswerSpan",
    )

    expected_has_suggested_span = (
        span_start is not None
        and span_length is not None
        and record["suggestedRawValue"] is not None
    )

    if has_suggested_span != expected_has_suggested_span:
        raise BundleValidationError(
            f"{location}.hasSuggestedSpan: значение не совпадает "
            "с наличием spanStart, spanLength и suggestedRawValue."
        )

    expected_span_end = (
        span_start + span_length
        if span_start is not None and span_length is not None
        else None
    )

    if span_end != expected_span_end:
        raise BundleValidationError(
            f"{location}.spanEnd: значение не совпадает с spanStart + spanLength."
        )

    expected_verified_answer_span = example_kind == "AcceptedSpan"

    if has_verified_answer_span != expected_verified_answer_span:
        raise BundleValidationError(
            f"{location}.hasVerifiedAnswerSpan: значение не соответствует "
            "exampleKind."
        )

    expected_split = calculate_expected_split(normalized_product_name)

    if split != expected_split:
        raise BundleValidationError(
            f"{location}: группа '{normalized_product_name}' находится в части "
            f"{split}, но алгоритм {EXPECTED_SPLIT_ALGORITHM_VERSION} "
            f"назначает её в {expected_split}."
        )

    return normalized_product_name


def validate_jsonl(
    raw_data: bytes,
    split: str,
    file_name: str,
    feedback_ids: set[str],
) -> SplitValidationResult:
    if raw_data and not raw_data.endswith(b"\n"):
        raise BundleValidationError(
            f"{file_name}: непустой JSONL-файл должен завершаться переводом строки."
        )

    try:
        text = raw_data.decode("utf-8")
    except UnicodeDecodeError as error:
        raise BundleValidationError(f"{file_name}: файл не является UTF-8.") from error

    product_groups: set[str] = set()
    example_count = 0

    for line_number, line in enumerate(text.splitlines(), start=1):
        if not line.strip():
            raise BundleValidationError(
                f"{file_name}, строка {line_number}: пустые строки запрещены."
            )

        try:
            value = json.loads(line)
        except json.JSONDecodeError as error:
            raise BundleValidationError(
                f"{file_name}, строка {line_number}: некорректный JSON: "
                f"{error.msg}"
            ) from error

        normalized_product_name = validate_record(
            value,
            split,
            file_name,
            line_number,
            feedback_ids,
        )

        product_groups.add(normalized_product_name)
        example_count += 1

    return SplitValidationResult(
        split=split,
        file_name=file_name,
        example_count=example_count,
        product_groups=frozenset(product_groups),
        sha256=calculate_sha256(raw_data),
    )


def validate_bundle(bundle_path: Path) -> BundleValidationResult:
    if not bundle_path.is_file():
        raise BundleValidationError(f"ZIP-пакет не найден: {bundle_path}")

    if not zipfile.is_zipfile(bundle_path):
        raise BundleValidationError(f"Файл не является ZIP-пакетом: {bundle_path}")

    with zipfile.ZipFile(bundle_path, mode="r") as archive:
        entry_names = [entry.filename for entry in archive.infolist()]

        if len(entry_names) != len(set(entry_names)):
            raise BundleValidationError(
                "ZIP содержит несколько записей с одинаковыми именами."
            )

        actual_files = set(entry_names)
        missing_files = sorted(EXPECTED_FILES - actual_files)
        unexpected_files = sorted(actual_files - EXPECTED_FILES)

        if missing_files:
            raise BundleValidationError(
                f"ZIP не содержит обязательные файлы: {', '.join(missing_files)}."
            )

        if unexpected_files:
            raise BundleValidationError(
                f"ZIP содержит неожиданные файлы: {', '.join(unexpected_files)}."
            )

        manifest = parse_manifest(archive.read("manifest.json"))

        bundle_format_version = require_string(
            manifest.get("bundleFormatVersion"),
            "manifest.bundleFormatVersion",
        )
        dataset_format_version = require_string(
            manifest.get("datasetFormatVersion"),
            "manifest.datasetFormatVersion",
        )
        split_algorithm_version = require_string(
            manifest.get("splitAlgorithmVersion"),
            "manifest.splitAlgorithmVersion",
        )

        if bundle_format_version != EXPECTED_BUNDLE_FORMAT_VERSION:
            raise BundleValidationError(
                "manifest.bundleFormatVersion: ожидалась версия "
                f"{EXPECTED_BUNDLE_FORMAT_VERSION}, получена "
                f"{bundle_format_version}."
            )

        if dataset_format_version != EXPECTED_DATASET_FORMAT_VERSION:
            raise BundleValidationError(
                "manifest.datasetFormatVersion: ожидалась версия "
                f"{EXPECTED_DATASET_FORMAT_VERSION}, получена "
                f"{dataset_format_version}."
            )

        if split_algorithm_version != EXPECTED_SPLIT_ALGORITHM_VERSION:
            raise BundleValidationError(
                "manifest.splitAlgorithmVersion: ожидался алгоритм "
                f"{EXPECTED_SPLIT_ALGORITHM_VERSION}, получен "
                f"{split_algorithm_version}."
            )

        require_string(
            manifest.get("finalizedUntilUtc"),
            "manifest.finalizedUntilUtc",
        )

        manifest_example_count = require_non_negative_int(
            manifest.get("exampleCount"),
            "manifest.exampleCount",
        )
        manifest_product_group_count = require_non_negative_int(
            manifest.get("productGroupCount"),
            "manifest.productGroupCount",
        )
        manifest_dataset_sha256 = require_sha256(
            manifest.get("datasetSha256"),
            "manifest.datasetSha256",
        )

        require_array(manifest.get("warnings"), "manifest.warnings")
        raw_split_manifests = require_array(
            manifest.get("splits"),
            "manifest.splits",
        )

        if len(raw_split_manifests) != len(EXPECTED_SPLITS):
            raise BundleValidationError(
                "manifest.splits: ожидалось ровно три части: "
                "Train, Validation и Test."
            )

        split_manifest_by_name: dict[str, dict[str, Any]] = {}

        for index, raw_split_manifest in enumerate(raw_split_manifests):
            split_manifest = require_object(
                raw_split_manifest,
                f"manifest.splits[{index}]",
            )
            split_name = require_string(
                split_manifest.get("split"),
                f"manifest.splits[{index}].split",
            )

            if split_name in split_manifest_by_name:
                raise BundleValidationError(
                    f"manifest.splits: часть {split_name} указана повторно."
                )

            split_manifest_by_name[split_name] = split_manifest

        feedback_ids: set[str] = set()
        split_results: list[SplitValidationResult] = []

        for expected_split, expected_file_name in EXPECTED_SPLITS:
            if expected_split not in split_manifest_by_name:
                raise BundleValidationError(
                    f"manifest.splits: отсутствует часть {expected_split}."
                )

            split_manifest = split_manifest_by_name[expected_split]
            manifest_file_name = require_string(
                split_manifest.get("fileName"),
                f"manifest.splits.{expected_split}.fileName",
            )

            if manifest_file_name != expected_file_name:
                raise BundleValidationError(
                    f"manifest.splits.{expected_split}.fileName: ожидался "
                    f"{expected_file_name}, получен {manifest_file_name}."
                )

            result = validate_jsonl(
                archive.read(expected_file_name),
                expected_split,
                expected_file_name,
                feedback_ids,
            )

            manifest_split_example_count = require_non_negative_int(
                split_manifest.get("exampleCount"),
                f"manifest.splits.{expected_split}.exampleCount",
            )
            manifest_split_product_group_count = require_non_negative_int(
                split_manifest.get("productGroupCount"),
                f"manifest.splits.{expected_split}.productGroupCount",
            )
            manifest_split_sha256 = require_sha256(
                split_manifest.get("sha256"),
                f"manifest.splits.{expected_split}.sha256",
            )

            if result.example_count != manifest_split_example_count:
                raise BundleValidationError(
                    f"{expected_file_name}: найдено {result.example_count} "
                    "примеров, но manifest указывает "
                    f"{manifest_split_example_count}."
                )

            if len(result.product_groups) != manifest_split_product_group_count:
                raise BundleValidationError(
                    f"{expected_file_name}: найдено "
                    f"{len(result.product_groups)} групп, но manifest указывает "
                    f"{manifest_split_product_group_count}."
                )

            if result.sha256 != manifest_split_sha256:
                raise BundleValidationError(
                    f"{expected_file_name}: SHA-256 не совпадает с manifest."
                )

            split_results.append(result)

        all_product_groups: set[str] = set()
        total_example_count = 0

        for result in split_results:
            overlap = all_product_groups.intersection(result.product_groups)

            if overlap:
                overlapping_group = sorted(overlap)[0]
                raise BundleValidationError(
                    f"Группа '{overlapping_group}' присутствует более чем "
                    "в одной части датасета."
                )

            all_product_groups.update(result.product_groups)
            total_example_count += result.example_count

        if total_example_count != manifest_example_count:
            raise BundleValidationError(
                f"В JSONL найдено {total_example_count} примеров, "
                f"но manifest указывает {manifest_example_count}."
            )

        if len(all_product_groups) != manifest_product_group_count:
            raise BundleValidationError(
                f"В JSONL найдено {len(all_product_groups)} групп, "
                f"но manifest указывает {manifest_product_group_count}."
            )

        canonical_dataset_description = "\n".join(
            f"{result.file_name}:{result.sha256}"
            for result in split_results
        )
        calculated_dataset_sha256 = calculate_sha256(
            canonical_dataset_description.encode("utf-8")
        )

        if calculated_dataset_sha256 != manifest_dataset_sha256:
            raise BundleValidationError(
                "manifest.datasetSha256 не соответствует SHA-256 трёх частей."
            )

    split_by_name = {result.split: result for result in split_results}
    readiness_blockers: list[str] = []

    if len(all_product_groups) < MINIMUM_RECOMMENDED_PRODUCT_GROUP_COUNT:
        readiness_blockers.append(
            "независимых групп товаров "
            f"{len(all_product_groups)}, требуется минимум "
            f"{MINIMUM_RECOMMENDED_PRODUCT_GROUP_COUNT}"
        )

    for split_name in ("Train", "Validation", "Test"):
        result = split_by_name[split_name]

        if result.example_count == 0:
            readiness_blockers.append(f"{split_name}-набор пуст")

        if len(result.product_groups) == 0:
            readiness_blockers.append(
                f"{split_name}-набор не содержит независимых групп"
            )

    return BundleValidationResult(
        example_count=total_example_count,
        product_group_count=len(all_product_groups),
        split_results=tuple(split_results),
        dataset_sha256=calculated_dataset_sha256,
        ready_for_training=not readiness_blockers,
        readiness_blockers=tuple(readiness_blockers),
    )


def print_result(bundle_path: Path, result: BundleValidationResult) -> None:
    print(f"Пакет: {bundle_path}")
    print("Структурная проверка: VALID PACKAGE")
    print(f"Примеров: {result.example_count}")
    print(f"Независимых групп: {result.product_group_count}")

    for split_result in result.split_results:
        print(
            f"{split_result.split}: "
            f"examples={split_result.example_count}, "
            f"groups={len(split_result.product_groups)}, "
            f"sha256={split_result.sha256}"
        )

    print(f"Dataset SHA-256: {result.dataset_sha256}")

    if result.ready_for_training:
        print("Training Gate: READY FOR TRAINING")
        print(
            "Это разрешает следующий offline-подэтап: обучение baseline-модели. "
            "Это ещё не означает готовность модели к production."
        )
        return

    print("Training Gate: VALID PACKAGE, NOT READY FOR TRAINING")
    print("Причины:")

    for blocker in result.readiness_blockers:
        print(f"- {blocker}")


def create_argument_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Проверяет frozen ZIP-пакет датасета распознавания "
            "названий товаров ElectronicCRM."
        )
    )
    parser.add_argument(
        "bundle",
        type=Path,
        help="Путь к catalog-recognition-dataset-*.zip",
    )

    return parser


def main() -> int:
    parser = create_argument_parser()
    arguments = parser.parse_args()
    bundle_path = arguments.bundle.expanduser().resolve()

    try:
        result = validate_bundle(bundle_path)
    except BundleValidationError as error:
        print(f"Структурная проверка: INVALID PACKAGE", file=sys.stderr)
        print(f"Ошибка: {error}", file=sys.stderr)
        return 1
    except (OSError, zipfile.BadZipFile) as error:
        print("Структурная проверка: INVALID PACKAGE", file=sys.stderr)
        print(f"Ошибка чтения пакета: {error}", file=sys.stderr)
        return 1

    print_result(bundle_path, result)

    if result.ready_for_training:
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(main())