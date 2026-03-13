import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { ToastProvider } from './components/Toast';
import ErrorBoundary from './components/ErrorBoundary';
import ProtectedRoute from './routes/ProtectedRoute';
import Header from './components/Header';
import Login from './pages/Login';
import Register from './pages/Register';
import VerifyEmail from './pages/VerifyEmail';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import Profile from './pages/Profile';
import ProductListPage from './pages/ProductListPage';
import ProductDetailPage from './pages/ProductDetailPage';
import CreateProductPage from './pages/CreateProductPage';
import SellEntryPage from './pages/SellEntryPage';
import CreateStorePage from './pages/CreateStorePage';
import MyStorePage from './pages/MyStorePage';
import MyProductsPage from './pages/MyProductsPage';
import EditProductPage from './pages/EditProductPage';
import './App.css';

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <AuthProvider>
          <ToastProvider>
            <div className="app">
              <Header />
              <Routes>
                {/* Product pages */}
                <Route path="/" element={<ProductListPage />} />
                <Route path="/products/:id" element={<ProductDetailPage />} />
                <Route
                  path="/sell"
                  element={
                    <ProtectedRoute>
                      <SellEntryPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sell/new"
                  element={
                    <ProtectedRoute>
                      <CreateProductPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sell/my-products"
                  element={
                    <ProtectedRoute>
                      <MyProductsPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sell/edit/:id"
                  element={
                    <ProtectedRoute>
                      <EditProductPage />
                    </ProtectedRoute>
                  }
                />

                {/* Store pages */}
                <Route
                  path="/stores/create"
                  element={
                    <ProtectedRoute>
                      <CreateStorePage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/stores/me"
                  element={
                    <ProtectedRoute>
                      <MyStorePage />
                    </ProtectedRoute>
                  }
                />

                {/* Auth pages */}
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/verify-email" element={<VerifyEmail />} />
                <Route path="/forgot-password" element={<ForgotPassword />} />
                <Route path="/reset-password" element={<ResetPassword />} />
                <Route
                  path="/profile"
                  element={
                    <ProtectedRoute>
                      <Profile />
                    </ProtectedRoute>
                  }
                />
              </Routes>
            </div>
          </ToastProvider>
        </AuthProvider>
      </Router>
    </ErrorBoundary>
  );
}

export default App;
