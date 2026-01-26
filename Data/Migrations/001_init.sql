CREATE TABLE deployments (
    id UUID PRIMARY KEY,
    application TEXT NOT NULL,
    version TEXT NOT NULL,
    station TEXT NOT NULL,
    source_image TEXT NOT NULL,
    target_image TEXT NOT NULL,
    status TEXT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    finished_at TIMESTAMPTZ
);

CREATE TABLE deployment_logs (
    id BIGSERIAL PRIMARY KEY,
    deployment_id UUID REFERENCES deployments(id) ON DELETE CASCADE,
    timestamp TIMESTAMPTZ NOT NULL,
    level TEXT NOT NULL,
    message TEXT NOT NULL
);

