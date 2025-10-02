﻿return {
  app = {
    title = "kUpdater",
    subtitle = "칼온라인"
  },
  button = {
    start        = "Start",
    exit         = "X",
    settings     = "Settings",
    website      = "Website"
  },
  status = {
    waiting          = "Checking version...",
    up_to_date       = "Already up to date (v{0}).",
    update_required  = "Update required: {0} → {1}",
    applying_update  = "Applying update...",
    update_applied   = "Update successfully applied.",
    update_failed    = "Update failed: {0}",
    downloading_pkg  = "Downloading update package...",
    extracting_files = "Extracting files...",
    update_complete  = "Update complete!"
  },
  info = {
      current_version = "Current Version: {0}",
      latest_version  = "Latest Version: {0}"
  },
  error = {
    network      = "Network error. Please check your connection.",
    file_missing = "Required file not found.",
    unauthorized = "You are not authorized to perform this action.",
    hash_mismatch = "Hash mismatch for {0}"
  },
  dialog = {
    confirm_exit = {
      title   = "Exit Application",
      message = "Are you sure you want to quit?"
    }
  }
}
