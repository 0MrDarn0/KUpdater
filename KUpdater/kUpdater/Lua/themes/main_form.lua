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


local function bounds(x, y, w, h)
  return function()
    local width, height = get_window_size()
    local rx = (x < 0) and (width + x) or x
    local ry = (y < 0) and (height + y) or y
    return { x = rx, y = ry, width = w, height = h }
  end
end

-- Hilfsfunktion für rechts-/unten-Anchor
local function anchor(x, y, w, h)
  return function()
    local winW, winH = get_window_size()

    local rx = (x < 0) and (winW + x) or x
    local ry = (y < 0) and (winH + y) or y
    local rw = (w < 0) and (winW + w - rx) or w
    local rh = (h < 0) and (winH + h - ry) or h

    return { x = rx, y = ry, width = rw, height = rh }
  end
end

-- Rückgabe der gesamten Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()

    -- Title
    local titleLabel = UILabel("lb_title",
        bounds(35, 0, 200, 40),
        "kUpdater",
        Font("Chiller", 40, "Italic"),
        Color.Orange)
    uiElement.Add(titleLabel)

    local subtitleLabel = UILabel("lb_subtitle",
        bounds(-115, 12, 200, 27),
        "칼온라인", 
        Font("Malgun Gothic", 13, "Bold"),
        Color.Gold)
    uiElement.Add(subtitleLabel)

    -- Buttons
    local btnClose = UIButton("btn_close",
      bounds(-35, 16, 18, 18),
      "X",
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "btn_exit",
      function() application_exit() end)
    uiElement.Add(btnClose)

    local btnStart = UIButton("btn_start",
      bounds(-150, -70, 97, 22),
      "Start",
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() start_game() end)
    uiElement.Add(btnStart)

    local btnSettings = UIButton("btn_settings",
      bounds(-255, -70, 97, 22),
      "Settings",
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() open_settings() end)
    uiElement.Add(btnSettings)

    local btnWebsite = UIButton("btn_website",
      bounds(-360, -70, 97, 22),
      "Website",
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() open_website("https://google.com") end)
    uiElement.Add(btnWebsite)

    -- Status-Label
    local statusLabel = UILabel("lb_update_status",
        anchor(27, -50, 200, 20),
        "Status: Waiting...",
        Font("Segoe UI", 10, "Regular"),
        Color.White)
    uiElement.Add(statusLabel)

    -- Progressbar
    local progressBar = UIProgressBar("pb_update_progress", anchor(27, -30, -27, 5))
    uiElement.Add(progressBar)

  end,


  on_update_status = function(message)
    update_status(message)
  end,


  on_update_progress = function(value)
    update_download_progress(value)
  end

}
