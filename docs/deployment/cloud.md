# Cloud Deployment (Azure, AWS, GCP)

---

## 1. Supported Providers

| Provider | Compute | Database | Secrets | Storage | SSL |
|----------|---------|----------|---------|---------|-----|
| Azure | App Service | Azure Database for PostgreSQL | Azure Key Vault | Azure Blob Storage | App Service native SSL |
| AWS | App Runner | Amazon RDS for PostgreSQL | AWS Secrets Manager | AWS S3 | App Runner native SSL via ACM |
| GCP | Cloud Run | Cloud SQL for PostgreSQL | Google Secret Manager | Google Cloud Storage | Cloud Run native HTTPS |

All three providers use Terraform for infrastructure provisioning. IaC configurations are in `infrastructure/azure/`, `infrastructure/aws/`, and `infrastructure/gcp/`.

---

## 2. Terraform State

Each provider stores its own Terraform state independently using provider-native storage. There is no cross-provider state dependency.

| Provider | State backend | Location |
|----------|--------------|----------|
| Azure | `azurerm` (Azure Blob Storage) | Configured via `state_resource_group_name`, `state_storage_account_name` variables; container `tfstate`, key `countorsell.tfstate` |
| AWS | `s3` | Configured via `state_bucket`, `state_key`, `region` variables |
| GCP | `gcs` | Configured via `state_bucket` variable; prefix `terraform/state` |

---

## 3. Azure

**Infrastructure modules:**
- `modules/app-service` - Azure App Service for the application container
- `modules/postgresql` - Azure Database for PostgreSQL
- `modules/key-vault` - Azure Key Vault for secrets
- `modules/storage` - Azure Blob Storage for backups

**Terraform provider version:** `hashicorp/azurerm ~> 3.0`

**Required GitHub repository secrets:**

| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | Service principal client ID (`ARM_CLIENT_ID`) |
| `AZURE_CLIENT_SECRET` | Service principal client secret (`ARM_CLIENT_SECRET`) |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID (`ARM_SUBSCRIPTION_ID`) |
| `AZURE_TENANT_ID` | Azure tenant ID (`ARM_TENANT_ID`) |

**Workflow trigger:** The `azure.yml` workflow runs on pushes and pull requests to `main` that modify files in `infrastructure/azure/**`. `terraform apply` runs only on push to `main` (not on PRs).

---

## 4. AWS

**Infrastructure modules:**
- `modules/app-runner` - AWS App Runner for the application container
- `modules/rds` - Amazon RDS for PostgreSQL
- `modules/secrets-manager` - AWS Secrets Manager
- `modules/s3` - AWS S3 for backups

**Terraform provider version:** `hashicorp/aws ~> 5.0`

**Required GitHub repository secrets:**

| Secret | Description |
|--------|-------------|
| `AWS_ACCESS_KEY_ID` | AWS IAM access key ID |
| `AWS_SECRET_ACCESS_KEY` | AWS IAM secret access key |
| `AWS_DEFAULT_REGION` | AWS region (e.g., `us-east-1`) |

**Workflow trigger:** The `aws.yml` workflow runs on pushes and pull requests to `main` that modify files in `infrastructure/aws/**`. `terraform apply` runs only on push to `main`.

---

## 5. GCP

**Infrastructure modules:**
- `modules/cloud-run` - GCP Cloud Run for the application container
- `modules/cloud-sql` - Cloud SQL for PostgreSQL
- `modules/secret-manager` - Google Secret Manager
- `modules/cloud-storage` - Google Cloud Storage for backups

**Terraform provider version:** `hashicorp/google ~> 5.0`

**Required GitHub repository secrets:**

| Secret | Description |
|--------|-------------|
| `GCP_SERVICE_ACCOUNT_KEY` | Service account JSON key (used by `google-github-actions/auth@v2`) |

**Workflow trigger:** The `gcp.yml` workflow runs on pushes and pull requests to `main` that modify files in `infrastructure/gcp/**`. `terraform apply` runs only on push to `main`.

---

## 6. GitHub Actions Workflows

All cloud workflows follow the same pattern: detect changes to the provider's `infrastructure/` subdirectory, run `terraform init` and `terraform plan` on every run, and run `terraform apply` only on pushes to `main`.

| Workflow file | Provider | Path filter | Secrets used |
|---------------|----------|------------|--------------|
| `.github/workflows/azure.yml` | Azure | `infrastructure/azure/**` | `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID` |
| `.github/workflows/aws.yml` | AWS | `infrastructure/aws/**` | `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_DEFAULT_REGION` |
| `.github/workflows/gcp.yml` | GCP | `infrastructure/gcp/**` | `GCP_SERVICE_ACCOUNT_KEY` |

The Docker workflow (`.github/workflows/docker.yml`) builds and pushes the application image to a user-specified registry on push to `main`. It requires `REGISTRY_HOST`, `REGISTRY_USERNAME`, and `REGISTRY_PASSWORD` secrets. If these are not set, the workflow skips the build and push step.

The PR workflow (`.github/workflows/pr.yml`) runs `dotnet build` and `dotnet test` on every pull request to `main`. It has no cloud or registry dependencies.

---

## 7. Updating on Cloud

Cloud deployment updates are triggered through the deployment infrastructure rather than a script.

**Process:**
1. Build a new Docker image with the updated application version
2. Push the image to the configured registry (automated via the `docker.yml` workflow on push to `main`)
3. Trigger a redeploy through the cloud provider:
   - **Azure App Service:** Redeploy via the Azure portal, CLI, or by triggering the `azure.yml` workflow
   - **AWS App Runner:** App Runner detects new image pushes automatically if configured with ECR, or trigger a redeploy via the console or CLI
   - **GCP Cloud Run:** Trigger a new revision deployment via the `gcp.yml` workflow or `gcloud run deploy`

Schema migrations run automatically on application startup, using the same pre-update backup and rollback logic as Docker deployments. See [docs/backup-restore.md](../backup-restore.md) for details.
