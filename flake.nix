{
  description = "Development environment for gha-dotnet-rflow";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            # Core Languages & Build Tools
            dotnet-sdk_9

            # Git & Version Control
            git
            pre-commit

            # Linting & Validation
            actionlint
            commitlint
            zizmor

            # Development Tools
            curl
            jq
            act
            gh
          ];

          shellHook = ''
            export DOTNET_CLI_TELEMETRY_OPTOUT=1
            export DOTNET_NOLOGO=1

            # Install pre-commit hooks if config exists
            if [ -f .pre-commit-config.yaml ]; then
              pre-commit install --install-hooks > /dev/null 2>&1 || true
            fi

            echo "gha-dotnet-rflow development environment"
            echo ".NET: $(dotnet --version)"
          '';
        };
      }
    );
}
