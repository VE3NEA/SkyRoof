# Satellite Monitoring

**Monitored Satellites** is the panel that shows the satellites on your monitored list. If you do not see it you may need to enable it using the `View` menu.

To add a satellite to the monitored list, choose a satellite from the satellite list, then choose the desired transmitter to monitor. Right click on the satellite name and select "Monitor Satellite"
This will add the selected satellite and transmitter to the monitored list.

You can adjust the priority of the monitored satellites by moving them up and down the list using the arrow buttons or by dragging and dropping them to a new position. You can change the monitored transmitter simply by selecting a different transmitter on that same satellite. The monitored list will update automatically.

The monitored list also shows the next pass countdown and elevation for each monitored satellite.
The `min elevation` slider allows you to set a minimum elevation requirement for a higher priority satellite to interrupt a pass in progress for a lower-priority satellite. This allows you to prevent a good pass from getting interrupted by a low elevation pass just becuase the new pass is on a higher priority satellite.

The monitored satellites panel also allows you to set up automatic recordings in either audio or I/Q format. The recordings are automatically saved in the same location as the manual audio recordings.

### Enable automatic tuning
The `Auto Tuning` button on the bottom toolbar starts and stops the auto tuning feature. Once you enable it a warning shows up in the toolbar to remind you that it is changing things by itself. You can also turn off auto tuning by clicking the stop button in this toolbar box.

### Antenna Rotators (not supported)
The auto tuning feature does not currently support rotators. Antenna tracking is automatically disabled any time satellites are changed. This will be implemented in a future version.