import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { WorkflowService, Workflow, WorkflowExecution, WorkflowStatistics, CreateWorkflowData, UpdateWorkflowData, ExecuteWorkflowData } from '../../services/workflowService';

interface WorkflowState {
  workflows: Workflow[];
  currentWorkflow: Workflow | null;
  executions: WorkflowExecution[];
  currentExecution: WorkflowExecution | null;
  statistics: WorkflowStatistics | null;
  isLoading: boolean;
  error: string | null;
  pagination: {
    page: number;
    pageSize: number;
    total: number;
  };
}

const initialState: WorkflowState = {
  workflows: [],
  currentWorkflow: null,
  executions: [],
  currentExecution: null,
  statistics: null,
  isLoading: false,
  error: null,
  pagination: {
    page: 1,
    pageSize: 20,
    total: 0,
  },
};

export const fetchWorkflows = createAsyncThunk(
  'workflow/fetchWorkflows',
  async ({ page = 1, pageSize = 20 }: { page?: number; pageSize?: number } = {}) => {
    const response = await WorkflowService.getWorkflows(page, pageSize);
    return { workflows: response.data, page, pageSize };
  }
);

export const fetchWorkflow = createAsyncThunk(
  'workflow/fetchWorkflow',
  async (id: string) => {
    const response = await WorkflowService.getWorkflow(id);
    return response.data;
  }
);

export const createWorkflow = createAsyncThunk(
  'workflow/createWorkflow',
  async (data: CreateWorkflowData) => {
    const response = await WorkflowService.createWorkflow(data);
    return response.data;
  }
);

export const updateWorkflow = createAsyncThunk(
  'workflow/updateWorkflow',
  async (data: UpdateWorkflowData) => {
    await WorkflowService.updateWorkflow(data);
    return data;
  }
);

export const deleteWorkflow = createAsyncThunk(
  'workflow/deleteWorkflow',
  async (id: string) => {
    await WorkflowService.deleteWorkflow(id);
    return id;
  }
);

export const executeWorkflow = createAsyncThunk(
  'workflow/executeWorkflow',
  async ({ id, data }: { id: string; data: ExecuteWorkflowData }) => {
    const response = await WorkflowService.executeWorkflow(id, data);
    return response.data;
  }
);

export const fetchWorkflowExecutions = createAsyncThunk(
  'workflow/fetchWorkflowExecutions',
  async ({ workflowId, page = 1, pageSize = 20 }: { workflowId: string; page?: number; pageSize?: number }) => {
    const response = await WorkflowService.getWorkflowExecutions(workflowId, page, pageSize);
    return response.data;
  }
);

export const fetchWorkflowStatistics = createAsyncThunk(
  'workflow/fetchWorkflowStatistics',
  async ({ startDate, endDate }: { startDate?: string; endDate?: string } = {}) => {
    const response = await WorkflowService.getStatistics(startDate, endDate);
    return response.data;
  }
);

const workflowSlice = createSlice({
  name: 'workflow',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setCurrentWorkflow: (state, action: PayloadAction<Workflow | null>) => {
      state.currentWorkflow = action.payload;
    },
    setCurrentExecution: (state, action: PayloadAction<WorkflowExecution | null>) => {
      state.currentExecution = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchWorkflows.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchWorkflows.fulfilled, (state, action) => {
        state.isLoading = false;
        state.workflows = action.payload.workflows;
        state.pagination.page = action.payload.page;
        state.pagination.pageSize = action.payload.pageSize;
      })
      .addCase(fetchWorkflows.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch workflows';
      })
      .addCase(fetchWorkflow.fulfilled, (state, action) => {
        state.currentWorkflow = action.payload;
      })
      .addCase(createWorkflow.fulfilled, (state, action) => {
        state.workflows.unshift(action.payload);
      })
      .addCase(updateWorkflow.fulfilled, (state, action) => {
        const index = state.workflows.findIndex(w => w.id === action.payload.id);
        if (index !== -1) {
          state.workflows[index] = { ...state.workflows[index], ...action.payload };
        }
        if (state.currentWorkflow?.id === action.payload.id) {
          state.currentWorkflow = { ...state.currentWorkflow, ...action.payload };
        }
      })
      .addCase(deleteWorkflow.fulfilled, (state, action) => {
        state.workflows = state.workflows.filter(w => w.id !== action.payload);
        if (state.currentWorkflow?.id === action.payload) {
          state.currentWorkflow = null;
        }
      })
      .addCase(fetchWorkflowExecutions.fulfilled, (state, action) => {
        state.executions = action.payload;
      })
      .addCase(fetchWorkflowStatistics.fulfilled, (state, action) => {
        state.statistics = action.payload;
      });
  },
});

export const { clearError, setCurrentWorkflow, setCurrentExecution } = workflowSlice.actions;
export default workflowSlice.reducer;
