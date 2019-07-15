# CloudStreamConsole
The console application of the android app https://github.com/LagradOst/CloudStream



HELP (Launch Options)
------

movie, tv, anime [PREFIX, only searches from one provider] 

[SEARCH FOR] 

-c [NUMBER, autoselects a searchresult] 

-e [NUMBER, autoselects an episode] 

-l [NONE, autoselects the latest episode]

-vlc [NONE, autolaunch vlc with link]

-mpv [NONE, autolaunch mpv with link] (Linux Only)

Examples
----------
Example: [ movie iron man -c 8 -vlc ] Launches iron man with vlc

Example: [ anime death note -c 2 -e 1 -mpv ] Launches first episode of death note with mpv

Example: [ the orville -c 2 -l ] Loads the links for the latest episode of the orville 

Example: [ movie iron man -vlc ] Autoplays a movie with vlc when you select the movie
