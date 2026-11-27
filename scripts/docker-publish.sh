#!/usr/bin/env bash
# Construit l'image Docker de l'API (MusicDataApi) et la publie sur Docker Hub.
#
# Build depuis la racine du repo (le Dockerfile copie toute la solution), tag `latest`
# et, si --version est fourni, un tag de version en plus pour pouvoir revenir en arrière.
# Refuse de publier depuis une autre branche que master ou avec des modifications non
# commitées, pour que l'image corresponde toujours à un commit identifiable.
#
# Usage :
#   ./scripts/docker-publish.sh                    # build + push vladdfpc/rokapi:latest
#   ./scripts/docker-publish.sh --version 1.4.0    # pousse aussi le tag 1.4.0
#   ./scripts/docker-publish.sh --no-push          # build local uniquement
#   ./scripts/docker-publish.sh --force            # ignore les vérifications branche / arbre propre
set -euo pipefail

# Surchargeable : DOCKER_IMAGE=monuser/monimage ./scripts/docker-publish.sh
readonly IMAGE="${DOCKER_IMAGE:-vladdfpc/rokapi}"

cyan()   { printf '\033[36m%s\033[0m\n' "$*"; }
yellow() { printf '\033[33m%s\033[0m\n' "$*"; }
green()  { printf '\033[32m%s\033[0m\n' "$*"; }
die()    { printf '\033[31m%s\033[0m\n' "$*" >&2; exit 1; }

usage() { sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'; }

version=''
push=true
force=false

while (($# > 0)); do
    case "$1" in
        -v|--version)
            [[ $# -ge 2 && -n "$2" ]] || die "--version attend une valeur (ex. 1.4.0)."
            version="$2"; shift 2 ;;
        --no-push) push=false; shift ;;
        -f|--force) force=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) usage >&2; die "Option inconnue : $1" ;;
    esac
done

command -v docker >/dev/null || die "docker introuvable dans le PATH."

cd "$(dirname "$0")/.."

if [[ "$force" == false ]]; then
    branch="$(git rev-parse --abbrev-ref HEAD)"
    [[ "$branch" == 'master' ]] || die "Branche courante '$branch' : publier depuis master (ou --force)."
    [[ -z "$(git status --porcelain --untracked-files=no)" ]] || die 'Modifications non commitées : commit ou --force.'
fi

commit="$(git rev-parse --short HEAD)"
tags=("$IMAGE:latest")
[[ -n "$version" ]] && tags+=("$IMAGE:$version")

cyan "Build de ${tags[*]} depuis $commit…"
build_args=(build -f MusicDataApi/Dockerfile --label "org.opencontainers.image.revision=$commit")
for t in "${tags[@]}"; do build_args+=(-t "$t"); done
docker "${build_args[@]}" .

if [[ "$push" == false ]]; then
    yellow 'Build terminé, push ignoré (--no-push).'
    exit 0
fi

for t in "${tags[@]}"; do
    cyan "Push de $t…"
    docker push "$t"
done

green "Publié : ${tags[*]} ($commit)."
