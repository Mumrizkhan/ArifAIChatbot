import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { userApi } from '../../services/api';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

interface UserState {
  users: User[];
  currentUser: User | null;
  isLoading: boolean;
  error: string | null;
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

const initialState: UserState = {
  users: [],
  currentUser: null,
  isLoading: false,
  error: null,
  totalCount: 0,
  currentPage: 1,
  pageSize: 20,
};

export const fetchUsers = createAsyncThunk(
  'user/fetchUsers',
  async ({ page, pageSize, search }: { page: number; pageSize: number; search?: string }) => {
    const response = await userApi.getUsers(page, pageSize, search);
    return response;
  }
);

export const createUser = createAsyncThunk(
  'user/createUser',
  async (userData: Partial<User>) => {
    const response = await userApi.createUser(userData);
    return response;
  }
);

export const updateUser = createAsyncThunk(
  'user/updateUser',
  async ({ id, data }: { id: string; data: Partial<User> }) => {
    const response = await userApi.updateUser(id, data);
    return response;
  }
);

export const deleteUser = createAsyncThunk(
  'user/deleteUser',
  async (id: string) => {
    await userApi.deleteUser(id);
    return id;
  }
);

export const assignRole = createAsyncThunk(
  'user/assignRole',
  async ({ userId, role }: { userId: string; role: string }) => {
    await userApi.assignRole(userId, role);
    return { userId, role };
  }
);

const userSlice = createSlice({
  name: 'user',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    setCurrentPage: (state, action: PayloadAction<number>) => {
      state.currentPage = action.payload;
    },
    setPageSize: (state, action: PayloadAction<number>) => {
      state.pageSize = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchUsers.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.isLoading = false;
        state.users = action.payload.data;
        state.totalCount = action.payload.totalCount;
        state.currentPage = action.payload.currentPage;
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch users';
      })
      .addCase(createUser.fulfilled, (state, action) => {
        state.users.unshift(action.payload);
        state.totalCount += 1;
      })
      .addCase(updateUser.fulfilled, (state, action) => {
        const index = state.users.findIndex(u => u.id === action.payload.id);
        if (index !== -1) {
          state.users[index] = action.payload;
        }
      })
      .addCase(deleteUser.fulfilled, (state, action) => {
        state.users = state.users.filter(u => u.id !== action.payload);
        state.totalCount -= 1;
      })
      .addCase(assignRole.fulfilled, (state, action) => {
        const index = state.users.findIndex(u => u.id === action.payload.userId);
        if (index !== -1) {
          state.users[index].role = action.payload.role;
        }
      });
  },
});

export const { clearError, setCurrentPage, setPageSize } = userSlice.actions;
export default userSlice.reducer;
