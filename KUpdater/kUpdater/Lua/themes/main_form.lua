-- Hintergrund-Konfiguration
local background_config = {
  top_left      = "main_tl_default.png",
  top_center    = "main_tc_default.png",
  top_right     = "main_tr_default.png",
  right_center  = "main_rc_default.png",
  bottom_right  = "main_br_default.png",
  bottom_center = "main_bc_default.png",
  bottom_left   = "main_bl_default.png",
  left_center   = "main_lc_default.png",
  fill_color    = "#101010"
}

local layout_config = {
  top_width_offset    = 7,
  bottom_width_offset = 21,
  left_height_offset  = 5,
  right_height_offset = 5,
  fill_pos_offset     = 5,
  fill_width_offset   = 12,
  fill_height_offset  = 10
}

-- Hilfsfunktion für Farben
local function get_color(name)
  local color_table = {
    orange = "#ffa500",
    gold   = "#ffd700",
    white  = "#ffffff"
  }
  return color_table[string.lower(name)] or "#ffffff"
end

    local titel_color = get_color("orange")
    local subtitle_color = get_color("gold")
    local btn_color = get_color("orange")


-- Rückgabe der gesamten Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  -- Init-Funktion direkt inline
  init = function()
    --local width, height = get_window_size()

    -- Title
    add_label("lb_title", "kUpdater", 35, -10, titel_color, "Chiller", 40, "Italic")
    add_label("lb_subtitle", "칼온라인",  -115, 12, subtitle_color, "Malgun Gothic", 13, "Bold")

    -- Buttons
    add_button("btn_close", "X", -35, 16, 18, 18,
      "Segoe UI", 10, "Regular", btn_color, "btn_exit",
      function() application_exit() 
    end)

    add_button("btn_start", "Start", -150, -70, 97, 22,
      "Segoe UI", 11, "Regular", btn_color, "btn_default",
      function() start_game() 
    end)

    add_button("btn_settings", "Settings", -255, -70, 97, 22,
      "Segoe UI", 11, "Regular", btn_color, "btn_default",
      function() open_settings() 
    end)

    add_button("btn_website", "Website", -360, -70, 97, 22,
      "Segoe UI", 11, "Regular", btn_color, "btn_default",
      function() open_website("https://google.com") 
    end)


    -- Status-Label
    add_label("lb_update_status", "Status: Waiting...", 27, -50, "#FFFFFF", "Segoe UI", 10, "Regular")
    -- Progressbar
    add_progressbar("pb_update_progress", 27, -30, -53, 5)

  end,

  -- Update-Callbacks
  on_update_status = function(message)
    update_label("lb_update_status", message)
  end,

  on_update_progress = function(value)
    update_progress("pb_update_progress", value)
  end

}
