// API Barrel File - Re-exports all modules
// Import from this file for backward compatibility with existing code

export * from './client';
export * from './auth';
export * from './hr';
export * from './inventory';
export * from './sales';
export * from './procurement';
export * from './finance';
export * from './mes';
export * from './planning';
export * from './esg';
export * from './system';

// Re-export hrApi from system module for backward-compatible aliases
import { hrApi } from './system';
export { hrApi };
export const attendanceApi = hrApi.attendance;
export const leaveRequestsApi = hrApi.leaveRequests;
export const payrollApi = hrApi.payroll;
