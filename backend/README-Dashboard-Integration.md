# Tenant Admin Dashboard - Backend Integration

This document explains how to test the real-time integration between the Tenant Admin dashboard and the backend API.

## Prerequisites

1. **Backend Server**: Make sure the ASP.NET Core backend is running on `http://localhost:5000`
2. **Frontend**: Open the Tenant Admin dashboard in your browser
3. **Authentication**: Set a mock authentication token for testing

## Setup Instructions

### 1. Start the Backend Server

```bash
cd backend
dotnet run
```

The server should start on `http://localhost:5000`

### 2. Configure Frontend for Testing

Open the browser developer console and set up mock authentication:

```javascript
// Set mock authentication token
localStorage.setItem('authToken', 'mock-jwt-token-for-testing');
localStorage.setItem('tenantId', 'default-tenant');
```

### 3. Open the Dashboard

Navigate to `modules/Tenant-Admin/home.html` in your browser.

## Testing the Integration

### 1. Test API Connectivity

The dashboard should automatically:
- Load dashboard statistics from `/api/tenantadmin/dashboard/stats`
- Load recent activity from `/api/tenantadmin/dashboard/recent-activity`
- Show loading states while fetching data

### 2. Test Real-Time Updates with SignalR

Open the browser console to see SignalR connection logs. The connection should:
- Connect to `/dashboardHub`
- Join the tenant group
- Be ready to receive real-time updates

### 3. Trigger Test Activities

Use the test endpoints to simulate real-time updates:

#### Trigger Test Activity
```bash
curl -X POST "http://localhost:5000/api/test/trigger-activity?tenantId=default-tenant"
```

#### Trigger Stats Update
```bash
curl -X POST "http://localhost:5000/api/test/trigger-stats-update?tenantId=default-tenant"
```

#### Trigger Low Stock Alert
```bash
curl -X POST "http://localhost:5000/api/test/trigger-low-stock?tenantId=default-tenant"
```

### 4. Expected Behavior

When you trigger test events:

1. **SignalR Connection**: You should see connection logs in the browser console
2. **Real-time Updates**: The dashboard should update immediately without page refresh
3. **Notifications**: Toast notifications should appear for new activities
4. **Data Updates**: Stats cards and activity feed should update in real-time

## Features Implemented

### Frontend (Tenant-Admin/home.html)
- ✅ API integration with authentication headers
- ✅ Loading states for all data fetching
- ✅ Error handling with user-friendly messages
- ✅ SignalR client for real-time updates
- ✅ Toast notifications for new activities
- ✅ Automatic reconnection handling
- ✅ Proper cleanup on logout

### Backend
- ✅ Dashboard API endpoints
- ✅ SignalR hub for real-time communication
- ✅ Dashboard notification service
- ✅ Test controller for integration testing
- ✅ CORS configuration for frontend access
- ✅ Dependency injection setup

## Troubleshooting

### Common Issues

1. **CORS Errors**: Ensure the backend CORS policy includes your frontend URL
2. **SignalR Connection Failed**: Check that the backend is running and accessible
3. **Authentication Errors**: Set mock tokens in localStorage as shown above
4. **404 Errors**: Verify all API endpoints are properly registered

### Browser Console Checks

Look for these console messages:
- `SignalR connected successfully` - Connection established
- `User connected: [connection-id]` - Backend received connection
- `Received real-time update: [data]` - Frontend received updates

## Next Steps

To make this production-ready:

1. **Database Integration**: Replace mock data with actual database queries
2. **Authentication**: Implement proper JWT authentication
3. **Multi-tenancy**: Add proper tenant isolation
4. **Error Handling**: Add comprehensive error logging
5. **Performance**: Add caching and optimization
6. **Security**: Add input validation and rate limiting

## API Endpoints

### Dashboard
- `GET /api/tenantadmin/dashboard/stats` - Get dashboard statistics
- `GET /api/tenantadmin/dashboard/recent-activity` - Get recent activities

### Testing
- `POST /api/test/trigger-activity` - Trigger test activity
- `POST /api/test/trigger-stats-update` - Trigger stats update
- `POST /api/test/trigger-low-stock` - Trigger low stock alert

### SignalR Hub
- `/dashboardHub` - Real-time communication hub
