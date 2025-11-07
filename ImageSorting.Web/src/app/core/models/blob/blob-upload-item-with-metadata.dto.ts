import { FileMetadataDto } from "./file-metadata.dto";

export interface BlobUploadItemWithMetadata {
    blobName: string;
    metadata: FileMetadataDto;
}