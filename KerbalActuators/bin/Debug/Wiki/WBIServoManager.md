            
The Servo Manager is designed to manage the states of one or more servos located in the part. The part module should be placed after the last servo controller part module in the config file. The manager is responsible for presenting the individual servo GUI panels as well as the GUI needed to create, load, update, delete, and play various sequences. These sequences are a way to programmatically control the positioning of various servos without having to manually enter in their positions.
        
## Fields

### maxWindowHeight
Maximum height of the GUI
### managerState
Current state of the manager
### sequenceID
Current sequence that's being played.
### snapshotID
Current snapshot
### runningEffectName
Name of the effect to play while a servo controller is running
## Methods


### ToggleGUI
This event shows or hides the servo manager GUI.

### TakeSnapshot
Takes a snapshot of the current state of the servo controllers
> #### Return value
> A SNAPSHOT ConfigNode containing the current state of the servo controllers

### PlaySequence(System.Int32)
Plays the desired sequence.
> #### Parameters
> **sequenceIndex:** An integer containing the desired sequence index.


### PlayHomeSequence
Plays the home sequence. Home sequence is the "stored" state of the part's servos.

### PlaySnapshot(System.Collections.Generic.List{ConfigNode})
Plays a list of supplied snapshots
> #### Parameters
> **snapshotList:** A list containing SNAPSHOT ConfigNode objects to play.


### PlaySnapshot(ConfigNode)
Plays a single snapshot
> #### Parameters
> **snapshotNode:** A SNAPSHOT ConfigNode containing servo state information


### PlaySnapshot(System.Int32)
Plays the desired snapshot from the current sequence
> #### Parameters
> **snapshotIndex:** An integer containing the desired snampshot index.


### AddSequence(ConfigNode)
Adds a new sequence node to the sequence list.
> #### Parameters
> **node:** A SEQUENCE_NODE ConfigNode containing the sequence to add.


### CreateHomeSequence
Uses the current servo states to define the "Home" sequence. When the user presses the Home button, the part's servos will return the mesh transforms to this recorded state.

### CreateHomeSequence(ConfigNode)
Creates a home sequence from the supplied config node.
> #### Parameters
> **node:** A SEQUENCE_NOD ConfigNode containing the new home sequence


