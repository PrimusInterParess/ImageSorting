import { BlobUploadItemWithMetadata } from "./blob-upload-item-with-metadata.dto";

export interface BlobUploadWithMetadataResponse {
    uploaded: number;
    items: BlobUploadItemWithMetadata[];
}