import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import type { AxiosError } from "axios";
import type { ApiError } from "../types";
import "./Auth.css";

export function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await login(email, password);
      navigate("/");
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      setError(
        axiosError.response?.data?.error || "Login failed. Please try again.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-about">
        <h2>⬡ IdentityHub</h2>
        <p>
          A Non-Human Identity management platform that helps security teams
          discover, track, and remediate machine identity risks — with native
          Jira integration.
        </p>
        <div className="auth-about-features">
          <div className="auth-about-feature">
            <strong>Jira Integration</strong>
            <span>
              Create, edit, comment on, and transition Jira tickets directly for
              your NHI organization tickets.
            </span>
          </div>
          <Link to="/docs#rest-api" className="auth-about-feature">
            <strong>External API</strong>
            <span>
              Support REST API for CI/CD pipelines and security scanners.
            </span>
          </Link>
          <div className="auth-about-feature">
            <strong>Multi-User &amp; Secure</strong>
            <span>
              Each user's data is fully isolated — private even within the same
              organization.
            </span>
          </div>
        </div>
      </div>
      <div className="auth-card">
        <h1>Sign In</h1>
        <p className="auth-subtitle">Access your NHI management dashboard</p>
        {error && <div className="auth-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Min. 8 characters"
              required
              minLength={8}
            />
          </div>
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>
        <p className="auth-footer">
          Don't have an account? <Link to="/register">Register</Link>
        </p>
      </div>
    </div>
  );
}
