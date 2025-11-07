import { BlobItemInfo } from "./blob-item-info";

export interface BlobListResponse {
    count: number;
    items: BlobItemInfo[];
}
