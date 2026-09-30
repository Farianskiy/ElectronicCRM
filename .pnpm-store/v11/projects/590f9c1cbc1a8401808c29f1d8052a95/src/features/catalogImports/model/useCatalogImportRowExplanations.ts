"use client";

import {
  useIsFetching,
  useIsMutating,
  useQuery,
  useQueryClient,
  type UseQueryResult,
} from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { getCatalogImportRowExplanations } from "../api/getCatalogImportRowExplanations";
import { catalogImportQueryKeys } from "./queryKeys";
import type {
  CatalogImportBatchDetails,
  GetCatalogImportRowsResponse,
} from "./types";

export function useCatalogImportRowExplanations(
  batchId: string,
  expectedVersion: number,
  rowsQuery: UseQueryResult<GetCatalogImportRowsResponse, Error>,
) {
  const queryClient = useQueryClient();

  const batchFetching = useIsFetching({
    queryKey: catalogImportQueryKeys.details(batchId),
    exact: true,
  });

  const saving = useIsMutating({
    mutationKey: catalogImportQueryKeys.saveRows(batchId),
  });

  const batchState = queryClient.getQueryState<CatalogImportBatchDetails>(
    catalogImportQueryKeys.details(batchId),
  );

  const rowIds = (rowsQuery.data?.items ?? []).map((row) => row.rowId);

  const enabled =
    batchId.length > 0 &&
    rowIds.length > 0 &&
    rowIds.length <= 25 &&
    saving === 0 &&
    batchFetching === 0 &&
    batchState?.status === "success" &&
    batchState.fetchStatus === "idle" &&
    !batchState.isInvalidated &&
    batchState.data?.version === expectedVersion &&
    rowsQuery.isSuccess &&
    rowsQuery.fetchStatus === "idle" &&
    !rowsQuery.isPlaceholderData;

  const query = useQuery({
    queryKey: [
      ...catalogImportQueryKeys.nameExplanations(batchId),
      expectedVersion,
      rowsQuery.dataUpdatedAt,
      rowIds,
    ],
    queryFn: ({ signal }) =>
      getCatalogImportRowExplanations(
        batchId,
        expectedVersion,
        rowIds,
        signal,
      ),
    enabled,
    staleTime: 0,
    gcTime: 60_000,
    retry: false,
    refetchOnWindowFocus: false,
  });

    let message = "Цвет — найденное объяснение, а не подтверждение значения. «Без объяснения» не означает ошибку.";

  if (saving > 0) {
    message = "Сохраняем изменения. Прежняя подсветка временно скрыта.";
  } else if (rowsQuery.isError || batchState?.status === "error") {
    message = "Не удалось обновить исходные данные для подсветки.";
  } else if (!enabled) {
    message = "Подсветка появится после обновления строк и версии пакета.";
  } else if (query.isError) {
    message = getApiErrorMessage(
      query.error,
      "Не удалось получить актуальную подсветку.",
    );
  } else if (query.fetchStatus === "paused") {
    message = "Подсветка ожидает восстановления соединения.";
  } else if (query.isPending || query.isFetching) {
    message = "Получаем объяснения текущей страницы…";
  }

  async function refresh(): Promise<void> {
    await queryClient.invalidateQueries({
      queryKey: catalogImportQueryKeys.nameExplanations(batchId),
      refetchType: "none",
    });

    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.details(batchId),
      }),
      queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.rowsRoot(batchId),
      }),
    ]);
  }

  return {
    data:
      enabled && query.isSuccess && query.fetchStatus === "idle"
        ? query.data
        : undefined,
    message,
    busy:
      saving > 0 ||
      batchFetching > 0 ||
      rowsQuery.isFetching ||
      query.isFetching,
    refresh,
  };
}