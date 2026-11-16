export interface SupplierPortalSession {
  supplierUserId: string;
  username: string;
  contactName: string | null;
  role: string;
  supplierCode: string;
  supplierName: string;
}

export interface SupplierComplaintSummary {
  complaintId: string;
  complaintNumber: string;
  purchaseOrderId: string | null;
  skuId: string | null;
  lotNumber: string | null;
  defectQuantity: number;
  ppm: number;
  defectType: string | null;
  priority: string;
  description: string | null;
  status: string;
  createdAt: string;
  capaNumber: string | null;
  capaStatus: string | null;
  responseDue: string | null;
  supplierRespondedAt: string | null;
}

export interface SupplierCapaRevision {
  revisionNumber: number;
  action: string;
  d1Team: string | null;
  d2ProblemDescription: string | null;
  d3InterimContainment: string | null;
  d4RootCause: string | null;
  d5PermanentAction: string | null;
  d6Verification: string | null;
  d7Prevention: string | null;
  d8Closure: string | null;
  createdAt: string;
}

export interface SupplierComplaintDetail extends SupplierComplaintSummary {
  capaId: string | null;
  isOverdue: boolean;
  d1Team: string | null;
  d2ProblemDescription: string | null;
  d3InterimContainment: string | null;
  d4RootCause: string | null;
  d5PermanentAction: string | null;
  d6Verification: string | null;
  d7Prevention: string | null;
  d8Closure: string | null;
  revisions: SupplierCapaRevision[];
}

export interface CapaResponseSubmission {
  d1Team: string;
  d2ProblemDescription: string;
  d3InterimContainment: string;
  d4RootCause: string;
  d5PermanentAction: string;
  d6Verification: string;
  d7Prevention: string;
  d8Closure: string;
}

export interface CapaResponseSaved {
  complaintId: string;
  capaId: string;
  capaNumber: string;
  status: string;
  revisionNumber: number;
  action: string;
  recordedAt: string;
}

export interface CapaAttachment {
  attachmentId: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: string;
  uploadedAt: string;
  scannedAt: string | null;
  revisionNumber: number | null;
}

export interface CapaAttachmentUpload {
  attachmentId: string;
  originalFileName: string;
  scanStatus: string;
  uploadedAt: string;
}

export interface RfqSummary {
  rfqId: string;
  rfqNumber: string;
  rfqDate: string;
  deadline: string | null;
  status: string;
  itemCount: number;
  quotedItemCount: number;
}

export interface RfqItem {
  skuId: string;
  skuNumber: string | null;
  skuName: string | null;
  quantity: number;
  unit: string | null;
  specifications: string | null;
  unitPrice: number | null;
  promisedDeliveryDate: string | null;
  leadTimeDays: number | null;
  minimumOrderQuantity: number | null;
  tradeTerms: string | null;
  notes: string | null;
  quotedAt: string | null;
}

export interface RfqDetail extends Omit<RfqSummary, 'itemCount' | 'quotedItemCount'> {
  currency: string;
  items: RfqItem[];
}

export interface PurchaseOrderSummary {
  purchaseOrderId: string;
  poNumber: string;
  poDate: string;
  expectedDeliveryDate: string | null;
  status: string;
  version: number;
  currency: string;
  totalAmount: number;
  itemCount: number;
  latestResponseType: string | null;
  latestResponseVersion: number | null;
  latestResponseAt: string | null;
}

export interface PurchaseOrderLine {
  purchaseOrderItemId: string;
  skuId: string;
  skuNumber: string | null;
  skuName: string | null;
  quantity: number;
  unit: string | null;
  unitPrice: number;
  subtotal: number;
}

export interface PurchaseOrderProposal {
  purchaseOrderItemId: string;
  proposedQuantity: number | null;
  proposedDeliveryDate: string | null;
}

export interface PurchaseOrderResponseHistory {
  responseId: string;
  responseType: string;
  reason: string | null;
  purchaseOrderVersion: number;
  submittedAt: string;
  items: PurchaseOrderProposal[];
}

export interface PurchaseOrderDetail extends PurchaseOrderSummary {
  paymentTerms: string | null;
  tradeTerms: string | null;
  notes: string | null;
  subtotal: number;
  taxAmount: number;
  items: PurchaseOrderLine[];
  responseHistory: PurchaseOrderResponseHistory[];
}

export interface QuoteLineSubmission {
  skuId: string;
  unitPrice: number;
  leadTimeDays: number;
  minimumOrderQuantity: number;
  promisedDeliveryDate: string | null;
  tradeTerms: string | null;
  notes: string | null;
}

export interface PurchaseOrderResponseSubmission {
  responseType: 'accepted' | 'change_requested' | 'rejected';
  reason?: string;
  items?: Array<{
    purchaseOrderItemId: string;
    proposedQuantity?: number;
    proposedDeliveryDate?: string;
  }>;
}

export interface SupplierApSubmissionLineRequest {
  goodsReceiptItemId: string;
  invoicedQuantity: number;
  unitPrice: number;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
}

export interface SupplierApSubmissionDraftRequest {
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  currency: string;
  invoiceAmount: number;
  taxAmount: number;
  totalAmount: number;
  purchaseOrderId: string;
  goodsReceiptId: string;
  lines: SupplierApSubmissionLineRequest[];
}

export interface SupplierApSubmissionAttachment {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: string;
  uploadedAt: string;
  scannedAt: string | null;
  revisionNumber: number;
}

export interface SupplierApSubmissionReview {
  id: string;
  action: string;
  reason: string | null;
  matchResult: string;
  createdAt: string;
  reviewerUserId: string | null;
}

export interface SupplierApSubmissionSummary {
  id: string;
  submissionNumber: string;
  status: string;
  currentRevisionNumber: number;
  supplierInvoiceNumber: string;
  supplierInvoiceDate: string;
  currency: string;
  totalAmount: number;
  matchResult: string;
  version: number;
  createdAt: string;
  submittedAt: string | null;
}

export interface SupplierApSubmissionDetail extends SupplierApSubmissionSummary {
  invoiceAmount: number;
  taxAmount: number;
  purchaseOrderId: string;
  purchaseOrderNumber: string;
  goodsReceiptId: string;
  goodsReceiptNumber: string;
  discrepancies: string[];
  approvedAt: string | null;
  accountsPayableId: string | null;
  accountsPayableNumber: string | null;
  lines: Array<SupplierApSubmissionLineRequest & {
    purchaseOrderItemId: string | null;
    skuNumber: string | null;
    skuName: string | null;
    acceptedQuantity: number;
  }>;
  attachments: SupplierApSubmissionAttachment[];
  reviews: SupplierApSubmissionReview[];
}

export interface SupplierApSubmissionPage {
  items: SupplierApSubmissionSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface SupplierApEligibleSourceLine {
  goodsReceiptItemId: string;
  purchaseOrderItemId: string | null;
  skuId: string;
  skuNumber: string | null;
  skuName: string | null;
  acceptedQuantity: number;
  previouslyInvoicedQuantity: number;
  availableQuantity: number;
  purchaseOrderUnitPrice: number;
  unit: string;
}

export interface SupplierApEligibleSource {
  purchaseOrderId: string;
  purchaseOrderNumber: string;
  goodsReceiptId: string;
  goodsReceiptNumber: string;
  currency: string;
  lines: SupplierApEligibleSourceLine[];
}

export interface SupplierApSubmissionResult {
  submissionId: string;
  submissionNumber: string;
  status: string;
  revisionNumber: number;
  version: number;
  matchResult: string;
  discrepancies: string[];
  submittedAt: string | null;
  accountsPayableId: string | null;
  accountsPayableNumber: string | null;
}

export interface SupplierApSubmissionAttachmentUpload {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: string;
  uploadedAt: string;
  scannedAt: string | null;
  revisionNumber: number;
}

export interface PortalApiResponse<T> {
  data: T;
  etag: string | null;
}
