using Godot;
using System;
using System.Diagnostics;


public partial class OobaboogaTest : Node2D
{
	RichTextLabel DialogueText;
	TextEdit InputText;
	
	
	private static AudioStreamPlayer TextSound;

	// The prompt to send to the AI chatbot
	private static string Prompt;
	// The initial prompt, containing instructions/example conversation to set the tone of the AI
	private static string Context;
	// Use to control when to send the Context (for example, 1st message)
	private static bool SendContext = true;
	
	private AnimationPlayer GaiaAnimationPlayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		GaiaAnimationPlayer = GetNode<AnimationPlayer>("Gaia/AnimationPlayer");

		TextSound = GetNode<AudioStreamPlayer>("CursorSound");
		DialogueText = GetNode<RichTextLabel>("TextBackdrop/RichTextLabel");
		InputText = GetNode<TextEdit>("InputBackdrop/TextInput");
		OobaTalker.TextUpdate += DisplayText;
		OobaTalker.TextFinished += FlagFinished;
		
		

		Context = @"
		Throughout this conversation, you are GAIA, a magical entity that is the source of all life on earth.
		You will respond as GAIA.  Following is some sample conversation that serves to set the tone of your conversation.
		It need not be adhered to exactly.
		
		Only young Will can use the Psycho Dash. You can smash walls and 
		obstacles by hurling yourself against them. Use the Attack Button to save 
		energy.
		Only young Will can use the Psycho Slider. You can now use the 
		Sliding attack to pass through small passage ways. Push the Attack Button
		while running.
		Only young Will can use the Spin Dash. Spin your body to send enemies
		flying and use the recoil to climb hills. use the Attack and LR Buttons for
		power.
		Dark Friar can now be used! The Dark Friar is the dark power that
		only the Dark Knight, Freedan, can use. Use the Aura Power to scorch a
		distant enemy. Use the Attack Button to save energy.
		The Earthquaker is a Dark Power that can only be used by Freedan,
		the Dark Knight. This causes earthquakes. The enemy won't be able to move
		for a long time.  Push the Attack Button when jumping down.
		Shadow's greatest power, the Firebird, will arise when you're one with the
		Light Knight. Only you can restore the Earth to its original condition. I'm
		putting all of my faith in you...
		I am Gaia, the source of all life. I will help you on your journey. 
		Only one with the Dark Power can see this space. You are the chosen one. 
		In the dark space you can record a travel journal. Stop there before you 
		depart.";
	}


	public override async void _Input(InputEvent @event)
	{
		if (@event is InputEventKey KeyEvent && KeyEvent.Pressed)
		{
			InputText.GrabFocus();

			if (InputText.Editable)
			{
				// If they press Enter, initiate the AI conversation
				if (KeyEvent.Keycode == Key.Enter)
				{
					// Don't let the user type in the box while the AI is thinking/responding
					InputText.Editable = false;
					// Static variable, empty it before populating it with a new message.
					Prompt = "";

					// Probably only want to send the context/example conversation the first time
					if (SendContext)
					{
						Prompt += Context + "\n";
						SendContext = false;
					}

					// Add the user's input to the prompt we're sending to the chatbot
					Prompt += InputText.Text;

                    Debug.WriteLine($"Sending the following prompt:\n\n{Prompt}");
                    OobaTalker.PromptAI(Prompt, 2048, 6);
					InputText.Clear();
				}
			
			}
		}
	}



	private void DisplayText(object sender, string TextToDisplay)
	{
		if (GaiaAnimationPlayer.CurrentAnimation == "Blink")
		{	
			Debug.WriteLine("starting speak animation");
			GaiaAnimationPlayer.CallDeferred("play", "Speak");
		}

		DialogueText.SetDeferred("text", TextToDisplay);
		TextSound.CallDeferred("play");
	}

	private void FlagFinished(object sender, EventArgs e)
	{
		GaiaAnimationPlayer.CallDeferred("stop");
		GaiaAnimationPlayer.CallDeferred("play", "Blink");
		Prompt += $"\nResponse: {DialogueText.Text}";
		InputText.SetDeferred("editable", true);
	}


	

}
