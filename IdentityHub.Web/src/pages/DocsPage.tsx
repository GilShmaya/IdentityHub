import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import "./css/Docs.css";

export function DocsPage() {
  const { hash } = useLocation();

  useEffect(() => {
    if (hash) {
      const el = document.getElementById(hash.slice(1));
      el?.scrollIntoView({ behavior: "smooth" });
    }
  }, [hash]);

  return (
    <div className="docs-container">
      <main className="docs-content docs-content-centered">
        <header className="docs-hero">
          <div className="docs-hero-tag">About</div>
          <h1>IdentityHub</h1>
          <p>
            A Non-Human Identity management platform that helps security teams
            discover, track, and remediate machine identity risks — with native
            Jira integration for streamlined workflows.
          </p>
        </header>

        <section className="docs-section">
          <h2>What is IdentityHub?</h2>
          <p>
            Organizations manage thousands of non-human identities: service
            accounts, API keys, service principals, OAuth clients, and
            certificates. When these identities become stale, over-privileged,
            or unrotated, they pose serious security risks.
          </p>
          <p>
            IdentityHub provides a centralized platform to report and track NHI
            findings as Jira tickets. Security teams can create, edit, comment
            on, and transition tickets directly from the web portal — while
            automated systems can use the external API to submit findings
            programmatically.
          </p>
        </section>

        <section className="docs-section">
          <h2>Key Features</h2>
          <div className="docs-feature-grid">
            <div className="docs-feature">
              <span className="docs-feature-icon">⬡</span>
              <h3>Jira Integration</h3>
              <p>
                Create, view, edit, comment on, and transition Jira tickets.
                Supports assignee, priority, and workflow status changes.
              </p>
            </div>
            <div className="docs-feature">
              <span className="docs-feature-icon">◈</span>
              <h3>External API</h3>
              <p>
                API key-authenticated REST API for CI/CD pipelines and scanners.
                Bulk create up to 50 tickets per request. Tickets created via the
                API are linked to your account and visible in the portal.
              </p>
            </div>
            <div className="docs-feature">
              <span className="docs-feature-icon">⚿</span>
              <h3>API Key Management</h3>
              <p>
                Generate, view, and revoke API keys from the portal. Each key is
                tied to your account for audit and access control.
              </p>
            </div>
            <div className="docs-feature">
              <span className="docs-feature-icon">△</span>
              <h3>Multi-User &amp; Secure</h3>
              <p>
                Each user's data is fully isolated — Jira configurations,
                tickets, and API keys are private even within the same
                organization.
              </p>
            </div>
          </div>
        </section>

        <section className="docs-section">
          <h2>Getting Started</h2>
          <div className="docs-steps">
            <div className="docs-step">
              <span className="docs-step-num">1</span>
              <div>
                <h3>Create an account</h3>
                <p>Register with your email and a strong password.</p>
              </div>
            </div>
            <div className="docs-step">
              <span className="docs-step-num">2</span>
              <div>
                <h3>Connect Jira</h3>
                <p>
                  Go to Jira Settings and enter your Jira email, API token, and
                  site URL.
                </p>
              </div>
            </div>
            <div className="docs-step">
              <span className="docs-step-num">3</span>
              <div>
                <h3>Generate an API key</h3>
                <p>
                  Go to API Keys to create a key for programmatic access via the
                  REST API.
                </p>
              </div>
            </div>
            <div className="docs-step">
              <span className="docs-step-num">4</span>
              <div>
                <h3>Create tickets</h3>
                <p>
                  Select a project, fill in the finding details, and submit.
                  Your ticket appears in Jira instantly.
                </p>
              </div>
            </div>
          </div>
        </section>

        <section id="rest-api" className="docs-section">
          <h2>REST API</h2>
          <p>
            IdentityHub exposes an external API for programmatic access.
            Authentication is required via an API key (generated in the portal
            under API Keys). Tickets created through the API are linked to your
            account and visible in the web portal.
          </p>

          <div className="docs-endpoint">
            <div className="docs-endpoint-header">
              <span className="docs-method docs-method-post">POST</span>
              <code>/api/v1/tickets</code>
            </div>
            <p>
              Bulk create 1–50 NHI finding tickets. Provide Jira credentials and
              an array of tickets in the request body.
            </p>
            <div className="docs-code-group docs-code-group-full">
              <div className="docs-code-block">
                <span className="docs-code-label">Example</span>
                <pre>{`curl -X POST http://localhost:5202/api/v1/tickets \\
  -H "Content-Type: application/json" \\
  -H "X-Api-Key: YOUR_API_KEY" \\
  -d '{
    "jiraEmail": "bot@company.com",
    "jiraApiToken": "ATATT3xFfGF0...",
    "jiraSiteUrl": "https://yoursite.atlassian.net",
    "tickets": [{
      "title": "Stale API key: prod-gateway",
      "description": "Last rotated 365 days ago.",
      "projectKey": "NHI",
      "priority": "High"
    }]
  }'`}</pre>
              </div>
            </div>
          </div>

          <div className="docs-endpoint">
            <div className="docs-endpoint-header">
              <span className="docs-method docs-method-get">GET</span>
              <code>/api/v1/tickets?projectKey={"{key}"}</code>
            </div>
            <p>
              Get the 10 most recent tickets for a project. Pass Jira
              credentials as headers.
            </p>
            <div className="docs-code-group docs-code-group-full">
              <div className="docs-code-block">
                <span className="docs-code-label">Example</span>
                <pre>{`curl http://localhost:5202/api/v1/tickets?projectKey=NHI \\
  -H "X-Api-Key: YOUR_API_KEY" \\
  -H "X-Jira-Email: bot@company.com" \\
  -H "X-Jira-Api-Token: ATATT3xFfGF0..." \\
  -H "X-Jira-Site-Url: https://yoursite.atlassian.net"`}</pre>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
