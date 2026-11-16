import type {
  AgentWipDecimalFactDto,
  AgentWipIntFactDto,
  AgentWipNullableDecimalFactDto,
  AgentWipStringFactDto,
} from '../api/agentWip';

export type AgentWipFactDto =
  | AgentWipStringFactDto
  | AgentWipIntFactDto
  | AgentWipDecimalFactDto
  | AgentWipNullableDecimalFactDto;

export interface AgentWipFactPresentation {
  value: string;
  unit: string | null;
  factId: string;
  source: string;
  asOf: string | null;
  notApplicableReason: string | null;
}

export function toAgentWipFactPresentation(fact: AgentWipFactDto): AgentWipFactPresentation {
  const isNumericFact = 'displayValue' in fact;

  return {
    value: isNumericFact
      ? fact.displayValue ?? '不適用'
      : fact.value,
    unit: fact.unit,
    factId: fact.factId,
    source: `${fact.sourceTable}.${fact.sourceField}`,
    asOf: fact.asOf,
    notApplicableReason: fact.notApplicableReason,
  };
}
