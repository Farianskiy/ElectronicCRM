import type { UserType } from "@/shared/api/authToken";

export type UserPermissionCode =
  | "AssistantUse"
  | "ProductsView"
  | "PriceCalculationsManage"
  | "PriceCalculationsExport"
  | "CatalogImportsCreate"
  | "CatalogImportsReview"
  | "ProductsEdit"
  | "PricesManage"
  | "StockManage"
  | "PriceListsManage"
  | "DictionariesManage"
  | "UsersManage";

export interface CurrentUserPermission {
  code: UserPermissionCode;
  group: string;
  label: string;
  isAllowed: boolean;
  isOverridden: boolean;
}

export interface CurrentUserAccessResponse {
  userId: string;
  userType: UserType;
  permissions: CurrentUserPermission[];
}

export const qualityWorkspacePermissions: readonly UserPermissionCode[] = [
  "CatalogImportsReview",
  "PriceListsManage",
  "DictionariesManage",
];

export const defaultPermissionsByUserType: Readonly<Record<UserType, readonly UserPermissionCode[]>> = {
  Regular: ["AssistantUse", "ProductsView"],
  Manager: ["AssistantUse", "ProductsView", "PriceCalculationsManage", "PriceCalculationsExport", "CatalogImportsCreate"],
  Technical: ["ProductsView", "CatalogImportsCreate", "CatalogImportsReview", "ProductsEdit", "PricesManage", "StockManage", "PriceListsManage", "DictionariesManage"],
  Administrator: ["UsersManage"],
  SystemDeveloper: ["AssistantUse", "ProductsView", "PriceCalculationsManage", "PriceCalculationsExport", "CatalogImportsCreate", "CatalogImportsReview", "ProductsEdit", "PricesManage", "StockManage", "PriceListsManage", "DictionariesManage", "UsersManage"],
  Admin: ["AssistantUse", "ProductsView", "PriceCalculationsManage", "PriceCalculationsExport", "CatalogImportsCreate", "CatalogImportsReview", "ProductsEdit", "PricesManage", "StockManage", "PriceListsManage", "DictionariesManage", "UsersManage"],
};