#!/bin/bash
# run from the ValheimPlus repo root dir

# Update this as necessary:
BEPINEXPACK_VALHEIM_VERSION="5.4.2105"

# Constants
TEMP_DIR="temp"

BEPINEXPACK_VALHEIM_DOWNLOAD_URL="https://gcdn.thunderstore.io/live/repository/packages/denikson-BepInExPack_Valheim-$BEPINEXPACK_VALHEIM_VERSION.zip"
BEPINEXPACK_VALHEIM_ZIP_FILE="$TEMP_DIR/denikson-BepInExPack_Valheim.zip"
BEPINEXPACK_VALHEIM_TEMP_DIR="$TEMP_DIR/denikson-BepInExPack_Valheim"
BEPINEXPACK_VALHEIM_CONTENT_DIR="$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim"

ASSEMBLY_PUBLICIZER_DOWNLOAD_URL="https://github.com/CabbageCrow/AssemblyPublicizer/releases/download/v1.1.0/AssemblyPublicizer.zip"
ASSEMBLY_PUBLICIZER_ZIP_FILE="$TEMP_DIR/AssemblyPublicizer.zip"
ASSEMBLY_PUBLICIZER_TEMP_DIR="$TEMP_DIR/AssemblyPublicizer"

YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

if [[ -z "$VALHEIM_INSTALL" ]]; then
    echo -e "${RED}The VALHEIM_INSTALL environment variable must be set.${NC}" 1>&2
    exit 1;
fi

echo "This will set up the ValheimPlus dev environment with the following options:"
echo -en "${YELLOW}"
echo "Valheim Install Location    = $VALHEIM_INSTALL"
echo "BepInExPack_Valheim version = $BEPINEXPACK_VALHEIM_VERSION"
echo -en "${NC}"
while true; do # todo flip to true
    read -p "Proceed? [y/n] " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit 1;;
        * ) echo "Please answer yes or no.";;
    esac
done

rm -rf "$TEMP_DIR"
mkdir "$TEMP_DIR"

# Create Bepinex bundle
curl -fso "$BEPINEXPACK_VALHEIM_ZIP_FILE" "$BEPINEXPACK_VALHEIM_DOWNLOAD_URL"
if [ $? -ne 0 ]; then
    echo -e "${RED}Couldn't download BepInExPack_Valheim version $BEPINEXPACK_VALHEIM_VERSION${NC}" 1>&2
    exit 1
fi

unzip -q "$BEPINEXPACK_VALHEIM_ZIP_FILE" -d "$BEPINEXPACK_VALHEIM_TEMP_DIR"
rm "$BEPINEXPACK_VALHEIM_ZIP_FILE" # todo remove

# copy bepinex pack to valheim install
cp -r "$BEPINEXPACK_VALHEIM_CONTENT_DIR/"* "$VALHEIM_INSTALL"

# move dlls from unstripped corlib to managed
cp "$VALHEIM_INSTALL/unstripped_corlib/"* "$VALHEIM_INSTALL/valheim_Data/Managed"

# Download AssemblyPublicizer
curl -Lfso "$ASSEMBLY_PUBLICIZER_ZIP_FILE" "$ASSEMBLY_PUBLICIZER_DOWNLOAD_URL"
if [ $? -ne 0 ]; then
    echo -e "${RED}Couldn't download AssemblyPublicizer${NC}" 1>&2
    exit 1
fi

unzip -q "$ASSEMBLY_PUBLICIZER_ZIP_FILE" -d "$ASSEMBLY_PUBLICIZER_TEMP_DIR"
rm "$ASSEMBLY_PUBLICIZER_ZIP_FILE" # todo remove

# run it on all `assembly_*.dll` files from "\Valheim\valheim_Data\Managed"
# This will create a new folder called "/publicized_assemblies/".
ASSEMBLY_PUBLICIZER_EXE=$(realpath "$ASSEMBLY_PUBLICIZER_TEMP_DIR/AssemblyPublicizer/AssemblyPublicizer.exe")
( \
    cd "$VALHEIM_INSTALL/valheim_Data/Managed/" \
        && find . | grep -e "assembly_.*\.dll" | xargs -d'\n' -n1 "$ASSEMBLY_PUBLICIZER_EXE" \
)

rm -rf "$TEMP_DIR"