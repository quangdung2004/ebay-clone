# CloneEbay - Auth Module Frontend

React 18 + Vite frontend for the CloneEbay authentication module.

## Tech Stack

- **React 18** - UI library
- **React Router v6** - Client-side routing
- **Axios** - HTTP client
- **Context API** - State management
- **Vite** - Build tool and dev server
- **CSS** - Styling (no external CSS framework)

## Features

✅ User Registration
✅ User Login
✅ Protected Routes
✅ User Profile Management
✅ Avatar Support
✅ JWT Token Authentication
✅ Automatic Token Refresh
✅ Error Handling
✅ Loading States
✅ Responsive Design

## Installation

```bash
npm install
```

## Running the Application

```bash
npm run dev
```

The application will start on `http://localhost:5173` (or another available port).

## Backend Requirements

The backend should be running on `http://localhost:5026`.

All API endpoints are proxied through Vite's dev server to avoid CORS issues.

## Project Structure

```
src/
├── api/
│   ├── axios.js          # Axios configuration with interceptors
│   └── authApi.js        # Auth API endpoints
├── components/
│   ├── Header.jsx        # Navigation header component
│   └── Header.css
├── context/
│   └── AuthContext.jsx   # Authentication context provider
├── pages/
│   ├── Login.jsx         # Login page
│   ├── Login.css         # Shared auth pages styling
│   ├── Register.jsx      # Registration page
│   ├── Profile.jsx       # User profile page
│   └── Profile.css
├── routes/
│   └── ProtectedRoute.jsx # Route protection wrapper
├── App.jsx               # Main app with routing
├── App.css
├── main.jsx              # Application entry point
└── index.css             # Global styles
```

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `GET /api/auth/me` - Get current user info
- `PUT /api/auth/profile` - Update user profile
- `POST /api/auth/logout` - Logout user

### API Response Format

All API responses follow this format:

```json
{
  "success": boolean,
  "code": string,
  "message": string,
  "data": any,
  "correlationId": string
}
```

## Routes

- `/` - Redirects to `/login`
- `/login` - Login page
- `/register` - Registration page
- `/profile` - User profile page (protected)

## Authentication Flow

1. **Registration/Login**: User submits credentials
2. **Token Storage**: Access token is saved to localStorage
3. **Auto-Authentication**: On app load, if token exists, user data is fetched
4. **Protected Routes**: Routes check for authentication before rendering
5. **Logout**: Token is removed and user is redirected to login

## Features Details

### Login Page
- Email and password inputs
- Form validation
- Error message display
- Loading state during submission
- Link to registration page

### Register Page
- Username, email, and password inputs
- Password confirmation validation
- Error message display
- Loading state during submission
- Link to login page

### Profile Page
- Display user information (username, email, role, avatar)
- Edit mode for username and avatar URL
- Save/cancel functionality
- Success/error message display
- Avatar image rendering with fallback

### Header Component
- Conditional navigation based on authentication
- Logout functionality
- Responsive design

## Environment Configuration

The Vite proxy is configured in `vite.config.js`:

```javascript
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5026',
      changeOrigin: true,
    },
  },
}
```

## Build for Production

```bash
npm run build
```

The production build will be created in the `dist/` directory.

## Development Tips

1. **Backend Must Be Running**: Ensure the backend is running on port 5026
2. **Token Management**: Tokens are stored in localStorage with key `accessToken`
3. **Error Handling**: All API errors are caught and displayed to the user
4. **Auto-Redirect**: Unauthorized requests (401) automatically redirect to login

## Troubleshooting

### CORS Issues
The Vite proxy handles CORS. If you still experience issues, ensure:
- Backend is running on `http://localhost:5026`
- Vite dev server is running
- Proxy is configured correctly in `vite.config.js`

### Token Issues
If authentication isn't working:
- Check browser console for errors
- Verify token is being saved to localStorage
- Check network tab for API requests/responses
- Ensure backend is returning proper token format

## License

MIT
