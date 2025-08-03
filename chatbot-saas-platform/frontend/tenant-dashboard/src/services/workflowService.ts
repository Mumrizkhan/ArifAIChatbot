import { apiClient } from './api';
import type { ApiResponse } from './api';

export interface Workflow {
  id: string;
  name: string;
  description: string;
  status: 'Draft' | 'Active' | 'Inactive' | 'Archived' | 'Error';
  version: string;
  isActive: boolean;
  definition?: WorkflowDefinition;
  variables: Record<string, any>;
  tags: string[];
  trigger?: WorkflowTrigger;
  settings?: WorkflowSettings;
  executionCount: number;
  lastExecutedAt?: string;
  templateId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowDefinition {
  steps: WorkflowStep[];
  connections: WorkflowConnection[];
  layout: WorkflowLayout;
}

export interface WorkflowStep {
  id: string;
  name: string;
  type: WorkflowStepType;
  configuration: Record<string, any>;
  position: { x: number; y: number };
  inputPorts: string[];
  outputPorts: string[];
  isStartStep: boolean;
  isEndStep: boolean;
  condition?: WorkflowStepCondition;
}

export interface WorkflowConnection {
  id: string;
  sourceStepId: string;
  sourcePort: string;
  targetStepId: string;
  targetPort: string;
  condition?: WorkflowConnectionCondition;
}

export interface WorkflowLayout {
  width: number;
  height: number;
  zoom: number;
  viewportPosition: { x: number; y: number };
}

export interface WorkflowTrigger {
  type: WorkflowTriggerType;
  configuration: Record<string, any>;
  isEnabled: boolean;
}

export interface WorkflowSettings {
  maxRetries: number;
  timeout: number;
  enableLogging: boolean;
  enableNotifications: boolean;
  errorHandling: 'Stop' | 'Continue' | 'Retry' | 'Escalate';
  customSettings: Record<string, any>;
}

export interface WorkflowStepCondition {
  expression: string;
  variables: Record<string, any>;
}

export interface WorkflowConnectionCondition {
  expression: string;
  variables: Record<string, any>;
}

export type WorkflowStepType = 'Start' | 'End' | 'Action' | 'Condition' | 'Loop' | 'Parallel' | 'Wait' | 'HttpRequest' | 'EmailSend' | 'DatabaseQuery' | 'ScriptExecution' | 'UserTask' | 'ServiceTask' | 'Timer' | 'Gateway' | 'SubWorkflow';

export type WorkflowTriggerType = 'Manual' | 'Scheduled' | 'Event' | 'Webhook' | 'FileUpload' | 'DatabaseChange' | 'MessageReceived';

export interface WorkflowExecution {
  id: string;
  workflowId: string;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled' | 'Timeout';
  inputData: Record<string, any>;
  outputData: Record<string, any>;
  startedAt: string;
  completedAt?: string;
  duration?: number;
  errorMessage?: string;
  triggerSource?: string;
  stepExecutions: WorkflowStepExecution[];
}

export interface WorkflowStepExecution {
  id: string;
  stepId: string;
  stepName: string;
  stepType: WorkflowStepType;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Skipped' | 'Cancelled';
  inputData: Record<string, any>;
  outputData: Record<string, any>;
  startedAt: string;
  completedAt?: string;
  duration?: number;
  errorMessage?: string;
  retryCount: number;
}

export interface WorkflowTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  definition: WorkflowDefinition;
  defaultVariables: Record<string, any>;
  tags: string[];
  isPublic: boolean;
  tenantId?: string;
  usageCount: number;
  rating: number;
}

export interface WorkflowStatistics {
  totalWorkflows: number;
  activeWorkflows: number;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  successRate: number;
  averageExecutionTime: number;
  stepTypeUsage: Record<WorkflowStepType, number>;
  executionsByWorkflow: Record<string, number>;
  periodStart: string;
  periodEnd: string;
}

export interface CreateWorkflowData {
  name: string;
  description: string;
  definition?: WorkflowDefinition;
  variables?: Record<string, any>;
  tags?: string[];
  trigger?: WorkflowTrigger;
  settings?: WorkflowSettings;
  templateId?: string;
}

export interface UpdateWorkflowData extends Partial<CreateWorkflowData> {
  id: string;
}

export interface ExecuteWorkflowData {
  inputData?: Record<string, any>;
  triggerSource?: string;
  waitForCompletion?: boolean;
}

export class WorkflowService {
  static async getWorkflows(page = 1, pageSize = 20): Promise<ApiResponse<Workflow[]>> {
    return apiClient.get(`/workflows?page=${page}&pageSize=${pageSize}`);
  }

  static async getWorkflow(id: string): Promise<ApiResponse<Workflow>> {
    return apiClient.get(`/workflows/${id}`);
  }

  static async createWorkflow(data: CreateWorkflowData): Promise<ApiResponse<Workflow>> {
    return apiClient.post('/workflows', data);
  }

  static async updateWorkflow(data: UpdateWorkflowData): Promise<ApiResponse<void>> {
    return apiClient.put(`/workflows/${data.id}`, data);
  }

  static async deleteWorkflow(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete(`/workflows/${id}`);
  }

  static async activateWorkflow(id: string): Promise<ApiResponse<void>> {
    return apiClient.post(`/workflows/${id}/activate`);
  }

  static async deactivateWorkflow(id: string): Promise<ApiResponse<void>> {
    return apiClient.post(`/workflows/${id}/deactivate`);
  }

  static async cloneWorkflow(id: string, newName: string): Promise<ApiResponse<Workflow>> {
    return apiClient.post(`/workflows/${id}/clone`, { newName });
  }

  static async executeWorkflow(id: string, data: ExecuteWorkflowData): Promise<ApiResponse<{ executionId: string }>> {
    return apiClient.post(`/workflows/${id}/execute`, data);
  }

  static async getWorkflowExecutions(workflowId: string, page = 1, pageSize = 20): Promise<ApiResponse<WorkflowExecution[]>> {
    return apiClient.get(`/workflows/${workflowId}/executions?page=${page}&pageSize=${pageSize}`);
  }

  static async getWorkflowExecution(executionId: string): Promise<ApiResponse<WorkflowExecution>> {
    return apiClient.get(`/workflows/executions/${executionId}`);
  }

  static async cancelExecution(executionId: string): Promise<ApiResponse<void>> {
    return apiClient.post(`/workflows/executions/${executionId}/cancel`);
  }

  static async retryExecution(executionId: string): Promise<ApiResponse<void>> {
    return apiClient.post(`/workflows/executions/${executionId}/retry`);
  }

  static async getStatistics(startDate?: string, endDate?: string): Promise<ApiResponse<WorkflowStatistics>> {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    return apiClient.get(`/workflows/statistics?${params.toString()}`);
  }

  static async getAvailableStepTypes(): Promise<ApiResponse<WorkflowStepType[]>> {
    return apiClient.get('/workflows/designer/step-types');
  }

  static async validateWorkflowDefinition(definition: WorkflowDefinition): Promise<ApiResponse<{ isValid: boolean; definition: WorkflowDefinition; message: string }>> {
    return apiClient.post('/workflows/designer/validate', definition);
  }

  static async getTemplates(category?: string, publicOnly = true): Promise<ApiResponse<WorkflowTemplate[]>> {
    const params = new URLSearchParams();
    if (category) params.append('category', category);
    params.append('publicOnly', publicOnly.toString());
    return apiClient.get(`/workflows/templates?${params.toString()}`);
  }

  static async createWorkflowFromTemplate(templateId: string, workflowName: string): Promise<ApiResponse<Workflow>> {
    return apiClient.post(`/workflows/templates/${templateId}/create-workflow`, { workflowName });
  }
}
