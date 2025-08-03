# API Services Documentation

This directory contains all the API service classes that handle communication with the backend. The services are organized by feature and provide a clean, type-safe interface for making API calls.

## Architecture

### Base API Client (`api.ts`)
The `ApiClient` class provides the foundation for all API communication:
- Automatic JWT token handling
- Request/response interceptors
- Error handling
- File upload support
- Type-safe responses

### Service Classes

#### AuthService (`authService.ts`)
Handles user authentication and profile management:
```typescript
import { AuthService } from '@/services';

// Login
const loginResponse = await AuthService.login({ email, password });

// Get current user
const user = await AuthService.getCurrentUser();

// Update profile
const updatedUser = await AuthService.updateProfile({ name: 'New Name' });
```

#### TenantService (`tenantService.ts`)
Manages tenant/organization settings:
```typescript
import { TenantService } from '@/services';

// Get current tenant
const tenant = await TenantService.getCurrentTenant();

// Update tenant settings
const settings = await TenantService.updateSettings({ allowRegistration: false });
```

#### TeamService (`teamService.ts`)
Handles team member management:
```typescript
import { TeamService } from '@/services';

// Get team members
const members = await TeamService.getTeamMembers({ page: 1, limit: 10 });

// Invite team member
const invitation = await TeamService.inviteTeamMember({
  email: 'user@example.com',
  role: 'member'
});
```

#### ChatbotService (`chatbotService.ts`)
Manages chatbot configuration and conversations:
```typescript
import { ChatbotService } from '@/services';

// Get chatbot config
const chatbot = await ChatbotService.getChatbot();

// Update knowledge base
const kbItem = await ChatbotService.createKnowledgeBaseItem({
  type: 'faq',
  title: 'How to login?',
  content: 'Use your email and password...',
  category: 'authentication',
  tags: ['login', 'auth']
});
```

#### AnalyticsService (`analyticsService.ts`)
Provides analytics and reporting data:
```typescript
import { AnalyticsService } from '@/services';

// Get overview metrics
const overview = await AnalyticsService.getOverview({
  dateFrom: '2024-01-01',
  dateTo: '2024-01-31'
});

// Export data
const exportUrl = await AnalyticsService.exportData({
  format: 'csv',
  type: 'users',
  dateRange: { from: '2024-01-01', to: '2024-01-31' }
});
```

#### SubscriptionService (`subscriptionService.ts`)
Handles billing and subscription management:
```typescript
import { SubscriptionService } from '@/services';

// Get current subscription
const subscription = await SubscriptionService.getCurrentSubscription();

// Update payment method
const paymentMethod = await SubscriptionService.addPaymentMethod(token);
```

## Usage in Redux Slices

Services integrate seamlessly with Redux Toolkit's `createAsyncThunk`:

```typescript
import { createAsyncThunk } from '@reduxjs/toolkit';
import { AuthService } from '@/services';

export const loginUser = createAsyncThunk(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      const response = await AuthService.login(credentials);
      return response.data;
    } catch (error) {
      return rejectWithValue(error.message);
    }
  }
);
```

## Error Handling

All services return consistent error objects:
```typescript
interface ApiError {
  message: string;
  status: number;
  code?: string;
}
```

Use the helper functions for error handling:
```typescript
import { handleApiError, isApiError } from '@/services';

try {
  await AuthService.login(credentials);
} catch (error) {
  if (isApiError(error)) {
    console.error(`API Error ${error.status}: ${error.message}`);
  } else {
    console.error(handleApiError(error));
  }
}
```

## Configuration

Set up your API base URL in the environment variables:
```env
VITE_API_BASE_URL=http://localhost:3000/api
```

## File Uploads

For file uploads, use the specialized upload methods:
```typescript
// Upload avatar
const formData = new FormData();
formData.append('avatar', file);
const result = await AuthService.uploadAvatar(file);

// Or use the base client directly
const result = await apiClient.upload('/custom/upload', formData);
```

## Type Safety

All services are fully typed with TypeScript interfaces. Import types as needed:
```typescript
import type { User, Tenant, TeamMember } from '@/services';
```

## Best Practices

1. **Use services in Redux slices**: Don't call services directly from components
2. **Handle errors properly**: Always use try/catch with rejectWithValue
3. **Type your responses**: Use the provided TypeScript interfaces
4. **Cache responses**: Let Redux manage the state, don't duplicate API calls
5. **Environment config**: Use environment variables for API configuration
