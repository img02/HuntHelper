# HuntHelper  [![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/HuntHelper)](https://github.com/imaginary-png/HuntHelper)

Hunt radar / map. A-Train recording. S Rank Utilities.  

A Dalamud plugin for Hunting

<details>
    <summary>Video</summary>
  
https://user-images.githubusercontent.com/70348218/184215756-fd223aad-ec4d-44cc-a02b-8b4eda07425e.mov

</details>
<details>
    <summary>Images</summary>
  
Record hunt trains and save taken spawn positions.  
![record spawn points and hunt trains](https://user-images.githubusercontent.com/70348218/187097275-8daee2fc-e5a3-4e22-88ad-58c988445d5e.png)

![take_spawn_points](https://user-images.githubusercontent.com/70348218/184554115-6f7d0c28-ed9c-4f3b-b35b-8b2c9405d9ea.png)

Counters for Spawning S Ranks  
![counters](https://user-images.githubusercontent.com/70348218/184554212-904efe4e-d3bf-4411-808a-57235d810996.png)

See all the hunts around you  
![udumUntitled](https://user-images.githubusercontent.com/70348218/184554139-8ad1f75f-5800-4d33-9dd1-6396e9823675.png)

Customise the UI  
![customisation](https://user-images.githubusercontent.com/70348218/184554412-edbfe473-9753-4314-8f35-cfa3d867d93f.png)

</details>

## IPC Usage 
Implemented by [im-scared](https://github.com/im-scared)

* `HH.GetTrainList` - returns the mob list from the train recorder.
* `HH.GetVersion` - returns a version number for the Hunt Helper IPC system. This allows other plugins to check the version of Hunt Helper's IPC functions, so they can avoid errors by not even calling the other IPC functions if the version isn't what they expect. This version only needs to updated with breaking changes.
* `HH.Enable` - this is a pub/sub signal that other plugins can subscribe to in order to know when Hunt Helper is enabled, ensuring other plugins don't waste time repeatedly calling IPC functions when Hunt Helper isn't around.
* `HH.Disable` - another pub/sub signal that other plugins can subscribe to, this time to know when Hunt Helper is being disabled. This allows other plugins to gracefully disable their Hunt Helper integration when Hunt Helper is turned off.


## Commands

/hh -> Opens main map window  
/hht -> Hunt train window  
/hhr -> Spawn position recording window  
/hhc -> Counter window

/hh1  
/hh2 -> Opens the map with preset size and position  
/hh1save  
/hh2save -> save the current window size and position to the preset number  

/hhn -> Gets the next mob in the train as a \<flag\> - Initiates teleport if enabled  
/hhna -> Gets the \<flag\> for the nearest Aetheryte to the next mob - Initiates teleport if enabled (overrides and disables /hhn teleport)   
