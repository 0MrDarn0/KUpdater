#!/bin/sh

# Uninstall Git commit-msg hook
HOOKS_DIR=".git/hooks"
HOOK_FILE="commit-msg"

# Check if inside a Git repo
if [ ! -d ".git" ]; then
  echo "❌ Not a Git repository. Run this from the project root."
  exit 1
fi

# Check if hook exists
if [ ! -f "$HOOKS_DIR/$HOOK_FILE" ]; then
  echo "ℹ️  No commit-msg hook found at $HOOKS_DIR/$HOOK_FILE"
  exit 0
fi

# Remove hook
rm "$HOOKS_DIR/$HOOK_FILE"
echo "🧹 commit-msg hook removed successfully!"
