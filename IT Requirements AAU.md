# IT Requirements

## Application Development and Data Architecture

The student teams may use appropriate front-end technologies and development frameworks, but the solution must be designed for integration with the ADD Data platform. Databricks must act as the governed data and processing backbone, rather than being treated as a passive file repository.

### Databricks application model

The preferred implementation is a Databricks App where this is technically suitable. Databricks Apps provide a managed application layer for building and running data applications close to governed data, SQL Warehouses and other Databricks services.

For new applications, the teams should use Databricks AppKit on GitHub as the reference implementation. AppKit is a TypeScript-based software development kit for Databricks applications. It supports a React client, a Node.js server, a plugin-based architecture, type-safe queries and integration with Databricks SQL Warehouses and Unity Catalog. It also provides standard capabilities for caching, telemetry, retry logic and error handling.

The expected application architecture is:

- **Client layer:** A React-based user interface, optionally using the AppKit UI components.
- **Server layer:** A Node.js backend based on AppKit, exposing controlled application programming interfaces and executing queries against Databricks.
- **Data layer:** Governed Delta tables, views or functions registered in Unity Catalog and queried through a Databricks SQL Warehouse.
- **Identity and access layer:** Application access based on a dedicated service identity and least-privilege permissions to explicitly approved Databricks resources.
- **Source control and deployment:** Application code maintained in Git with documented build, configuration and deployment instructions.

AppKit supports a React client that communicates with a Node.js server through HTTP and Server-Sent Events. The server can execute analytical queries against Databricks SQL Warehouses and provide REST interfaces to the client.

### Integration requirements

The application must use Databricks through one or more controlled integration patterns:

- Read approved Gold or Silver datasets through a Databricks SQL Warehouse.
- Write application-generated records to dedicated, approved Delta tables.
- Execute data transformations through Databricks jobs, workflows or governed SQL logic.
- Consume analytical or machine-learning outputs created in Databricks.
- Expose approved results through the application backend rather than giving the browser direct access to underlying tables.
- Use Delta Sharing when data must be consumed outside the Databricks workspace and direct application integration is not appropriate.

Credentials, access tokens and connection strings must not be embedded in source code or exposed to the client. The application must use an approved identity and secret-management pattern. Permissions must be limited to the specific catalog, schema, tables, SQL Warehouse and other resources required by the use case.

Applications must not connect directly to source or production systems where the required data is already available through the ADD Data platform. This ensures that access control, lineage, quality controls and data ownership remain anchored in Unity Catalog. Our target platform design uses Unity Catalog for central permissions, lineage, metadata and discovery, with access managed through Entra ID groups and additional protection for personally identifiable information where required.

### Medallion architecture

Data processing in the ADD Data platform follows the medallion architecture. Data is progressively refined through Bronze, Silver and Gold layers so that ingestion, validation, business logic and application consumption remain clearly separated. The purpose is to improve data quality and reliability while retaining traceability to the original source.

#### Bronze layer

The Bronze layer contains raw data ingested from source systems, files, application programming interfaces or approved manual inputs. Data is preserved as close as practical to its original form, including relevant ingestion metadata such as source, ingestion timestamp and pipeline execution identifier.

Student applications should generally not read directly from Bronze. Access to this layer may be permitted for specific data-engineering tasks, but Bronze data must not be presented directly to users or treated as application-ready data.

#### Silver layer

The Silver layer contains cleaned, validated and standardised data. This is where schemas, data types, identifiers, duplicate handling and required quality controls are applied.

Silver data may be used for detailed analysis, model development and application logic where the Gold layer does not provide sufficient detail. Any application using Silver data must document why the detailed dataset is required and must not reproduce transformation logic that belongs in the platform.

#### Gold layer

The Gold layer contains curated data products, business entities, metrics, aggregations and application-ready datasets. Gold is the preferred consumption layer for Databricks Apps, dashboards and externally shared data.

Applications should read from documented Gold tables or views wherever possible. Business definitions and reusable calculations should be implemented once in the Gold layer rather than recreated independently in application code. The ADD Data platform design treats Gold datasets as governed data products with documented ownership, monitoring and reuse through internal publication or Delta Sharing.

### Handling data generated by applications

Data generated by the application must be written to dedicated application schemas or tables and must not overwrite shared Silver or Gold datasets directly. Generated data may include user input, workflow status, annotations, classifications, recommendations, model outputs, feedback and application logs.

The student team must define:

- The purpose and structure of each output table.
- The source data and processing logic used to create each output.
- The relationship between generated records and source records.
- The relevant retention and deletion requirements.
- Whether the output may contain personal, operationally sensitive or security-relevant information.
- The process for promoting useful outputs from an application-specific area into a governed Silver or Gold data product.

If generated data is intended for reuse, it must pass an agreed validation and governance step before being published as a shared data product. Application writes must therefore remain separate from certified platform tables until the generated data, ownership and quality controls have been reviewed.

### Required design principle

The application should be replaceable without replacing the underlying data product, and the data product should be reusable without depending on the original application. User interface logic, application services, data processing and governed storage must therefore remain separate components with documented interfaces.

The expected outcome is a reusable prototype that demonstrates this integration pattern: governed data moves through Bronze, Silver and Gold; the application consumes approved Silver or Gold datasets; generated results are written to dedicated controlled tables; and access is managed through Unity Catalog and application-specific identities.
