# Local two-player prototype

Open `Assets/Scenes/RealGame.unity`, press Play, and click inside the Game view.

| Character | Controls |
| --- | --- |
| Yellow Player | WASD |
| Magenta Player2 | Arrow keys |
| Cyan SecondCharacter | Stationary touch target |

Both players support smooth eight-direction movement at equal diagonal and straight speed. Opposite keys cancel on each axis. Players collide with walls and each other. Either player can touch the cyan target to make it disappear; touching the other playable character does not hide that character.

The movement component's **Controls** dropdown selects WASD or Arrow Keys per player. Speed, acceleration, and deceleration remain editable. Player2 has a separate prefab at `Assets/Prefabs/Player2.prefab`.

The existing menu currently loads `Game`, not `RealGame`. For this prototype, open RealGame directly. Menu flow has not been changed.

Start from the initial scene state and choose **Game Jam > Check Local Two Players (while playing)** for a programmatic physics check. Also test WASD and arrows simultaneously on your keyboard; keyboard hardware can limit certain combinations of held keys.
