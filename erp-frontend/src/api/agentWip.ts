import { api } from './client';
import type { ApiResponse } from './client';

export interface AgentWipStringFactDto {
  factId: string;
  value: string;
  sourceTable: string;
  sourceField: string;
  asOf: string | null;
  unit: string | null;
  notApplicableReason: string | null;
  dataClassification: string;
}

export interface AgentWipIntFactDto {
  factId: string;
  value: number;
  displayValue: string;
  sourceTable: string;
  sourceField: string;
  asOf: string | null;
  unit: string | null;
  notApplicableReason: string | null;
  dataClassification: string;
}

export interface AgentWipDecimalFactDto {
  factId: string;
  value: number;
  displayValue: string;
  sourceTable: string;
  sourceField: string;
  asOf: string | null;
  unit: string | null;
  notApplicableReason: string | null;
  dataClassification: string;
}

export interface AgentWipNullableDecimalFactDto {
  factId: string;
  value: number | null;
  displayValue: string | null;
  sourceTable: string;
  sourceField: string;
  asOf: string | null;
  unit: string | null;
  notApplicableReason: string | null;
  dataClassification: string;
}

export interface AgentWipOperationDto {
  operationSequence: AgentWipIntFactDto;
  status: AgentWipStringFactDto;
}

export interface AgentWipFinalOperationDto {
  operationSequence: AgentWipIntFactDto;
  status: AgentWipStringFactDto;
  plannedQuantity: AgentWipDecimalFactDto;
  goodQuantity: AgentWipDecimalFactDto;
  defectQuantity: AgentWipDecimalFactDto;
  defectRatePercent: AgentWipNullableDecimalFactDto;
  remainingQuantity: AgentWipDecimalFactDto;
}

export interface AgentWipWorkOrderDto {
  workOrderNumber: AgentWipStringFactDto;
  productNumber: AgentWipStringFactDto;
  productName: AgentWipStringFactDto;
  productionUnit: AgentWipStringFactDto;
  status: AgentWipStringFactDto;
  authorizedOperations: AgentWipOperationDto[];
  finalOperation: AgentWipFinalOperationDto | null;
}

export const agentWipApi = {
  getWorkOrder: (workOrderNumber: string, sessionId: string) =>
    api.get<ApiResponse<AgentWipWorkOrderDto>>(
      `/agent-wip/work-orders/${encodeURIComponent(workOrderNumber)}`,
      { headers: { 'X-Agent-Session-Id': sessionId } },
    ),
};
