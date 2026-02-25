# Quick Start Guide

## Prerequisites

- Node.js installed (v18+ recommended)
- Backend API running on http://localhost:5026

## Setup & Run

1. **Install dependencies** (already done):
   ```bash
   npm install
   ```

2. **Start development server**:
   ```bash
   npm run dev
   ```

3. **Open browser**:
   - Navigate to: `http://localhost:5173`
   - You should see the login page

## Testing the Application

### 1. Register a New User
- Go to the Register page
- Fill in:
  - Username: `testuser`
  - Email: `test@example.com`
  - Password: `password123`
  - Confirm Password: `password123`
- Click "Create Account"
- You should be redirected to the Profile page

### 2. View Profile
- After successful registration/login, you'll see:
  - Your username
  - Your email
  - Your role
  - Avatar (if provided)

### 3. Edit Profile
- Click "Edit Profile" button
- Update your username or add an avatar URL
  - Example avatar URL: `https://i.pravatar.cc/300`
- Click "Save Changes"

### 4. Logout
- Click the "Logout" button in the header
- You'll be redirected to the login page
- Token will be removed from localStorage

### 5. Login Again
- Go to Login page
- Enter your email and password
- Click "Sign In"
- You'll be redirected to your Profile page

## Project Files Overview

```
src/
├── api/
│   ├── axios.js          ← Axios config with interceptors
│   └── authApi.js        ← API endpoint functions
│
├── components/
│   ├── Header.jsx        ← Navigation header
│   └── Header.css
│
├── context/
│   └── AuthContext.jsx   ← Global auth state
│
├── pages/
│   ├── Login.jsx         ← Login page
│   ├── Register.jsx      ← Registration page
│   ├── Profile.jsx       ← User profile page
│   ├── Login.css         ← Shared auth styling
│   └── Profile.css
│
├── routes/
│   └── ProtectedRoute.jsx ← Route guard component
│
├── App.jsx               ← Main app + routing
├── main.jsx              ← Entry point
└── index.css             ← Global styles
```

## Key Features

✅ **Registration** - Create new account with username, email, password
✅ **Login** - Sign in with email and password
✅ **Protected Routes** - Profile page requires authentication
✅ **Profile Management** - View and edit user profile
✅ **JWT Authentication** - Token-based auth with automatic header injection
✅ **Error Handling** - Display API errors to users
✅ **Loading States** - Show loading indicators during API calls
✅ **Auto-Redirect** - Redirect to login on 401 errors
✅ **Logout** - Clear token and redirect to login

## API Response Handling

All API responses are automatically processed:

```javascript
// Success response
{
  success: true,
  code: "AUTH001",
  message: "Login successful",
  data: {
    user: { ... },
    accessToken: "..."
  },
  correlationId: "..."
}

// Error response
{
  success: false,
  code: "AUTH002",
  message: "Invalid credentials",
  data: null,
  correlationId: "..."
}
```

## Troubleshooting

**Problem**: Can't connect to backend
- **Solution**: Ensure backend is running on `http://localhost:5026`

**Problem**: CORS errors
- **Solution**: Vite proxy should handle this. Check `vite.config.js`

**Problem**: Token not persisting
- **Solution**: Check browser localStorage for `accessToken` key

**Problem**: 401 errors
- **Solution**: Token might be expired. Try logging in again

## Development Commands

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm preview

# Run linter
npm run lint
```

## Notes

- The app uses **localStorage** to store the access token
- The token is automatically added to all API requests via Axios interceptor
- 401 responses automatically clear the token and redirect to login
- All routes except `/login` and `/register` are protected

Enjoy building with CloneEbay! 🚀
