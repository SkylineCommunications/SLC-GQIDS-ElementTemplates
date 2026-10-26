# Building and publishing

Open `ElementTemplates.sln` in Visual Studio with the DataMiner development
tools, or build it with `dotnet build ElementTemplates.sln -c Release`.
The package project produces the combined `.dmapp` and Catalog metadata archive
under `bin\Release\net48\DataMinerBuild`.

## Package contents

`PackageContent\ProjectReferences.xml` includes the sibling GQI project only.
The install hook installs that source and copies the two Assistant definitions
from `SetupContent\Assistant` to the documented Assistant custom-data-tool
directory. The build stages these definitions from the repository's `Assistant`
folder; edit those canonical files, not the generated setup copies.

Install and upgrade overwrite only the two owned tool files. They do not change
template assignments, device values, or unrelated Assistant tools. Assistant
must already be installed to discover and use the tools.

## Catalog publishing

The publish profile and `CatalogInformation\manifest.yml` retain the existing
Catalog item ID. In Visual Studio, configure the organization key using
**Manage User Secrets**, then use the package project's **Publish** profile.
The configured key name is `skyline:sdk:dataminertoken`; never put its value
in source control.

Publishing requires separate authorization. Building does not publish, deploy,
or prove runtime acceptance. See the repository README for the current host
verification status and API limitations.
