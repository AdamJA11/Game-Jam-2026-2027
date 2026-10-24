extends Node2D

var Random #A Var to Store Random Int In so the Bot can choose Rock, Paper, Scissors
var Round #What Player Chose this Round

@onready var CatBot = $"Cat Hand" #Bot Hands
@onready var Anim = $"Cat Hand/AnimationPlayer" # Bot Animation

@onready var Rock = $Buttons/Options/Rock #Rock Button
@onready var Paper = $Buttons/Options/Paper #Paper Button
@onready var Scissors = $Buttons/Options/Scissors #Scissors Button

@onready var Restart = $Buttons/Restart #Restart Button

@onready var Text = $Buttons/Text #Text Label

func _on_rock_pressed():
	Random = randi_range(0, 2) #Randomize a Number from 0 -> 2
	Rock.disabled = true #Disable Button
	Paper.disabled = true #Disable Button
	Scissors.disabled = true #Disable Button
	Anim.play("Show Hands") #Play Show Hands Animation
	Round = 0 #Player Chose Rock
	Text.visible = true #Show Text

func _on_paper_pressed():
	Random = randi_range(0, 2) #Randomize a Number from 0 -> 2
	Rock.disabled = true #Disable Button
	Paper.disabled = true #Disable Button
	Scissors.disabled = true #Disable Button
	Anim.play("Show Hands") #Play Show Hands Animation
	Round = 1 #Player Chose Paper
	Text.visible = true #Show Text

func _on_scissors_pressed():
	Random = randi_range(0, 2) #Randomize a Number from 0 -> 2
	Rock.disabled = true #Disable Button
	Paper.disabled = true #Disable Button
	Scissors.disabled = true #Disable Button
	Anim.play("Show Hands") #Play Show Hands Animation
	Round = 2 #Player Chose Scissors
	Text.visible = true #Show Text

func _on_restart_pressed():
	Rock.disabled = false #Enable Button
	Paper.disabled = false #Enable Button
	Scissors.disabled = false #Enable Button
	Anim.play("Hide Hands") #Play Hide Hands Animation
	Random = 3 #No more Bugs HeHe
	Round = 3 #No more Bugs HeHe
	Text.visible = false #Hide Text

func _ready():
	Anim.play("Hide Hands") #I used this on func ready because there was a bug the Hand shows up at start

func _process(_delta):
	if Random == 0: #What the CatBot Chose 0 = Rock
		CatBot.frame = 1 #CatBot Posing Rock
	elif Random == 1: #What the CatBot Chose 1 = Paper
		CatBot.frame = 0 #CatBot Posing Paper
	elif Random == 2: #What the CatBot Chose 2 = Scissors
		CatBot.frame = 2 #CatBot Posing Scissors

	#Win Situation
	if Round == 0 and Random == 2: #CatBot Chose Scissors and Player Chose Rock
		Text.text = "Cat Chose Scissors \n You Won😿"
	if Round == 1 and Random == 0: #CatBot Chose Rock and Player Chose Paper
		Text.text = "Cat Chose Rock \n You Won😿"
	if Round == 2 and Random == 1: #CatBot Chose Paper and Player Chose Scissors
		Text.text = "Cat Chose Paper \n You Won😿"

	#Lose Situation
	if Round == 0 and Random == 1: #CatBot Chose Paper and Player Chose Rock
		Text.text = "Cat Chose Paper \n You Lose😹"
	if Round == 1 and Random == 2: #CatBot Chose Scissors and Player Chose Paper
		Text.text = "Cat Chose Scissors \n You Lose😹"
	if Round == 2 and Random == 0: #CatBot Chose Rock and Player Chose Scissors
		Text.text = "Cat Chose Rock \n You Lose😹"

	#Tie Situation
	if Round == 0 and Random == 0: #Both Chose Rock
		Text.text = "Both Chose Rock \n That's A Tie😸"
	if Round == 1 and Random == 1: #Both Chose Paper
		Text.text = "Both Chose Paper \n That's A Tie😸"
	if Round == 2 and Random == 2: #Both Chose Scissors
		Text.text = "Both Chose Scissors \n That's A Tie😸"
