import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';

export interface TeamMember {
  id: string;
  name: string;
  email: string;
  role: 'admin' | 'agent' | 'viewer';
  avatar?: string;
  status: 'active' | 'inactive' | 'pending';
  lastLogin?: Date;
  permissions: string[];
  skills: string[];
  languages: string[];
  maxConversations: number;
  isOnline: boolean;
}

interface TeamState {
  members: TeamMember[];
  invitations: {
    id: string;
    email: string;
    role: string;
    status: 'pending' | 'accepted' | 'expired';
    createdAt: Date;
  }[];
  isLoading: boolean;
  error: string | null;
}

const initialState: TeamState = {
  members: [],
  invitations: [],
  isLoading: false,
  error: null,
};

export const fetchTeamMembers = createAsyncThunk(
  'team/fetchMembers',
  async () => {
    const response = await fetch('/api/team/members', {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch team members');
    }

    return response.json();
  }
);

export const inviteTeamMember = createAsyncThunk(
  'team/inviteMember',
  async ({ email, role }: { email: string; role: string }) => {
    const response = await fetch('/api/team/invite', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify({ email, role }),
    });

    if (!response.ok) {
      throw new Error('Failed to invite team member');
    }

    return response.json();
  }
);

export const updateTeamMember = createAsyncThunk(
  'team/updateMember',
  async ({ id, updates }: { id: string; updates: Partial<TeamMember> }) => {
    const response = await fetch(`/api/team/members/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
      body: JSON.stringify(updates),
    });

    if (!response.ok) {
      throw new Error('Failed to update team member');
    }

    return response.json();
  }
);

export const removeTeamMember = createAsyncThunk(
  'team/removeMember',
  async (id: string) => {
    const response = await fetch(`/api/team/members/${id}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to remove team member');
    }

    return id;
  }
);

const teamSlice = createSlice({
  name: 'team',
  initialState,
  reducers: {
    updateMemberStatus: (state, action: PayloadAction<{ id: string; isOnline: boolean }>) => {
      const member = state.members.find(m => m.id === action.payload.id);
      if (member) {
        member.isOnline = action.payload.isOnline;
      }
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchTeamMembers.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchTeamMembers.fulfilled, (state, action) => {
        state.isLoading = false;
        state.members = action.payload.members;
        state.invitations = action.payload.invitations;
      })
      .addCase(fetchTeamMembers.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Failed to fetch team members';
      })
      .addCase(inviteTeamMember.fulfilled, (state, action) => {
        state.invitations.push(action.payload);
      })
      .addCase(updateTeamMember.fulfilled, (state, action) => {
        const index = state.members.findIndex(m => m.id === action.payload.id);
        if (index !== -1) {
          state.members[index] = action.payload;
        }
      })
      .addCase(removeTeamMember.fulfilled, (state, action) => {
        state.members = state.members.filter(m => m.id !== action.payload);
      });
  },
});

export const { updateMemberStatus, clearError } = teamSlice.actions;
export default teamSlice.reducer;
