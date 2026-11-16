import { api, ApiResponse } from './client';

export interface EmissionRecordDto { id: string; sourceId?: string; sourceCode?: string; sourceName?: string; scope?: string; period: string; periodStart?: string; periodEnd?: string; activityData: number; activityUnit?: string; emissionFactor?: number; calculatedEmissions: number; emissionUnit?: string; status?: string; createdAt?: string; }
export interface EmissionSummaryDto { scope1?: number; scope2?: number; scope3?: number; total?: number; }
export interface EmissionSourceDto { id: string; sourceCode: string; sourceName: string; scope: string; category: string; description: string; isActive: boolean; }
export interface EmissionFactorDto { id: string; factorCode: string; factorName: string; unit: string; factorValue: number; unitOfMeasure: string; scope: string; source: string; region: string; year: number; isVerified: boolean; }
export interface CarbonReductionTargetDto { id: string; targetCode: string; targetName: string; scope: string; baselineEmissions: number; targetReductionPercent: number; targetEmissions: number; baseYear: number; targetYear: number; currentEmissions: number; achievedReductionPercent: number; description: string; status: string; }
export interface SupplierSustainabilityDto { id: string; supplierId: string; supplierCode: string; supplierName: string; carbonFootprint: number; carbonUnit: string; renewableEnergyPercent: number; sustainabilityScore: number; rating: string; lastAssessmentDate: string; nextAssessmentDate?: string; certification: string; improvementPlan: string; hasIso14001: boolean; hasIso14064: boolean; status: string; }
export interface EnergyConsumptionDto { id: string; energyType: string; scope: string; consumption: number; unit: string; carbonFactor: number; calculatedEmissions: number; period: string; }
export interface AiInsightDto { id: string; type: string; title: string; description: string; severity: string; recommendation?: string; potentialReduction?: number; }
export interface EsgReportDto { id: string; reportCode: string; reportName: string; reportType: string; framework: string; reportPeriodStart: string; reportPeriodEnd: string; totalScope1Emissions: number; totalScope2Emissions: number; totalScope3Emissions: number; totalEmissions: number; emissionIntensity: number; intensityUnit: string; status: string; isVerified: boolean; verifier: string; verifiedAt?: string; publishedAt?: string; }

export const esgApi = {
  emissionSummary: () => api.get<ApiResponse<EmissionSummaryDto>>('/esg/emission-summary'),
  emissionRecords: (params?: Record<string, string>) => api.get<ApiResponse<EmissionRecordDto[]>>('/esg/emission-records', { params }),
  createEmissionRecord: (data: Record<string, unknown>) => api.post<ApiResponse>('/esg/emission-records', data),
  emissionSources: () => api.get<ApiResponse<EmissionSourceDto[]>>('/esg/emission-sources'),
  createEmissionSource: (data: Record<string, unknown>) => api.post<ApiResponse>('/esg/emission-sources', data),
  emissionFactors: (params?: Record<string, string>) => api.get<ApiResponse<EmissionFactorDto[]>>('/esg/emission-factors', { params }),
  reductionTargets: () => api.get<ApiResponse<CarbonReductionTargetDto[]>>('/esg/reduction-targets'),
  supplierSustainability: () => api.get<ApiResponse<SupplierSustainabilityDto[]>>('/esg/supplier-sustainability'),
  energyConsumption: () => api.get<ApiResponse<EnergyConsumptionDto[]>>('/esg/energy-consumption'),
  aiInsights: (params?: Record<string, string>) => api.get<ApiResponse<AiInsightDto[]>>('/esg/ai-insights', { params }),
  reports: () => api.get<ApiResponse<EsgReportDto[]>>('/esg/reports'),
  calculateEmissions: (data: Record<string, unknown>) => api.post<ApiResponse<number>>('/esg/calculate-emissions', data),
};
