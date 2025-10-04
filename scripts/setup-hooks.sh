#!/bin/sh

# Setup Git commit-msg hook
HOOKS_DIR=".git/hooks"
HOOK_FILE="commit-msg"
SOURCE_HOOK="scripts/$HOOK_FILE"

# Check if inside a Git repo
if [ ! -d ".git" ]; then
  echo "❌ Not a Git repository. Run this from the project root."
  exit 1
fi

# Check if hook source exists
if [ ! -f "$SOURCE_HOOK" ]; then
  echo "❌ Hook file not found: $SOURCE_HOOK"
  exit 1
fi

# Check if hook already installed
if [ -f "$HOOKS_DIR/$HOOK_FILE" ]; then
  echo "ℹ️  Hook already exists at $HOOKS_DIR/$HOOK_FILE"
  echo "🔄 Overwriting..."
fi

# Install hook
cp "$SOURCE_HOOK" "$HOOKS_DIR/$HOOK_FILE"
chmod +x "$HOOKS_DIR/$HOOK_FILE"
echo "✅ commit-msg hook installed successfully!"
