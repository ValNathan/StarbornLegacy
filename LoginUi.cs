using Godot;
using System;

public partial class LoginUi : Control
{
	private LineEdit emailInput;
	private LineEdit passwordInput;
	private LineEdit usernameInput;
	private Button loginButton;
	private Button toggleModeButton;
	private Label titleLabel;
	private Label statusLabel;
	private HttpRequest httpRequest;
	
	private bool isSignupMode = false;
	
	private const string FIREBASE_API_KEY = "AIzaSyBc8y6lVw56vFTmSm4EjlZ88FildeWvLvQ";
	private const string FIREBASE_PROJECT_ID = "starborn-legacy";
	private string currentAuthToken = "";
	private string currentRequestType = "";
	
	public override void _Ready()
	{
		emailInput = GetNode<LineEdit>("LoginContainer/EmailInput");
		passwordInput = GetNode<LineEdit>("LoginContainer/PasswordInput");
		usernameInput = GetNode<LineEdit>("LoginContainer/UsernameInput");
		loginButton = GetNode<Button>("LoginContainer/LoginButton");
		toggleModeButton = GetNode<Button>("LoginContainer/ToggleModeButton");
		titleLabel = GetNode<Label>("LoginContainer/TitleLabel");
		statusLabel = GetNode<Label>("LoginContainer/StatusLabel");
		httpRequest = GetNode<HttpRequest>("HttpRequest");
		
		toggleModeButton.Pressed += OnToggleModePressed;
		loginButton.Pressed += OnLoginPressed;
		
		httpRequest.RequestCompleted += OnHttpRequestCompleted;
		
		if (httpRequest == null)
		{
			GD.PrintErr("HttpRequest est null ! Vérifiez le noeud dans la scène.");
		}
		else
		{
			GD.Print("HttpRequest trouvé avec succès");
		}
	}
	
	private void OnToggleModePressed()
	{
		isSignupMode = !isSignupMode;
		
		if (isSignupMode)
		{
			titleLabel.Text = "CRÉATION COMPTE";
			toggleModeButton.Text = "Se connecter";
			usernameInput.Visible = true;
			loginButton.Text = "S'INSCRIRE";
		}
		else
		{
			titleLabel.Text = "CONNEXION SYSTÈME";
			toggleModeButton.Text = "Créer un compte";
			usernameInput.Visible = false;
			loginButton.Text = "SE CONNECTER";
		}
		
		statusLabel.Text = "";
	}
	
	private void OnLoginPressed()
	{
		statusLabel.Text = "";
		
		if (isSignupMode)
		{
			if (ValidateSignupInputs())
			{
				statusLabel.Text = "Création du compte...";
				statusLabel.Modulate = Colors.Yellow;
				SetUIEnabled(false);
				SignUpUser();
			}
		}
		else
		{
			if (ValidateLoginInputs())
			{
				statusLabel.Text = "Connexion en cours...";
				statusLabel.Modulate = Colors.Yellow;
				SetUIEnabled(false);
				SignInUser();
			}
		}
	}
	
	private bool ValidateLoginInputs()
	{
		if (string.IsNullOrWhiteSpace(emailInput.Text))
		{
			statusLabel.Text = "Veuillez saisir votre email";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		if (string.IsNullOrWhiteSpace(passwordInput.Text))
		{
			statusLabel.Text = "Veuillez saisir votre mot de passe";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		return true;
	}
	
	private bool ValidateSignupInputs()
	{
		if (string.IsNullOrWhiteSpace(usernameInput.Text))
		{
			statusLabel.Text = "Veuillez saisir un nom d'utilisateur";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		if (string.IsNullOrWhiteSpace(emailInput.Text))
		{
			statusLabel.Text = "Veuillez saisir votre email";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		if(!System.Text.RegularExpressions.Regex.IsMatch(emailInput.Text, @"^[^@\s]+@[^@\s]+\.[^@/s]+$"))
		{
			statusLabel.Text = "Format d'email invalide";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		if (string.IsNullOrWhiteSpace(passwordInput.Text))
		{
			statusLabel.Text = "Veuillez saisir votre mot de passe";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		string password = passwordInput.Text;
		if (password.Length < 8 || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"\d") || !System.Text.RegularExpressions.Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
		{
			statusLabel.Text = "Mot de passe doit comporter 8 caractères minimum dont: 1 minuscule, 1 majuscule, 1 chiffre et 1 symbole";
			statusLabel.Modulate = Colors.Red;
			return false;
		}
		
		return true;
	}
	
	private void SignUpUser()
	{
		currentRequestType = "signup";
		string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FIREBASE_API_KEY}";
		
		var requestData = new Godot.Collections.Dictionary
		{
			["email"] = emailInput.Text.Trim(),
			["password"] = passwordInput.Text,
			["returnSecureToken"] = true
		};
		
		string jsonString = Json.Stringify(requestData);
		string[] headers = { "Content-Type: application/json" };
		
		httpRequest.Request(url, headers, HttpClient.Method.Post, jsonString);
	}
	
	private void SignInUser()
	{
		currentRequestType = "signin";
		string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FIREBASE_API_KEY}";
		
		var requestData = new Godot.Collections.Dictionary
		{
			["email"] = emailInput.Text.Trim(),
			["password"] = passwordInput.Text,
			["returnSecureToken"] = true
		};
		
		string jsonString = Json.Stringify(requestData);
		string[] headers = { "Content-Type: application/json" };
		
		httpRequest.Request(url, headers, HttpClient.Method.Post, jsonString);
	}
	
	private void OnHttpRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		SetUIEnabled(true);
		
		if (responseCode == 200)
		{
			if (currentRequestType == "signup")
			{
				var json = Json.ParseString(System.Text.Encoding.UTF8.GetString(body));
				var responseData = json.AsGodotDictionary();
			
				string userId = responseData["localId"].AsString();
				string email = responseData["email"].AsString();
				currentAuthToken = responseData["idToken"].AsString();
				
				statusLabel.Text = "Création du profil utilisateur...";
				statusLabel.Modulate = Colors.Yellow;
				CreateUserProfile(userId, usernameInput.Text.Trim(), email);
			}
			else if (currentRequestType == "create_profile")
			{
				statusLabel.Text = "Compte créé avec succès !";
				statusLabel.Modulate = Colors.Green;
			}
			else if (currentRequestType == "signin")
			{
				statusLabel.Text = "Connexion réussie !";
				statusLabel.Modulate = Colors.Green;
			}
		}
		else
		{
			string errorBody = System.Text.Encoding.UTF8.GetString(body);
			GD.PrintErr($"Code: {responseCode}, Body: {errorBody}");
			
			var json = Json.ParseString(errorBody);
			var errorData = json.AsGodotDictionary();
			
			if (errorData.ContainsKey("error"))
			{
				var error = errorData["error"].AsGodotDictionary();
				if (error.ContainsKey("message"))
				{
					string errorMessage = error["message"].AsString();
					GD.PrintErr($"Message d'erreur: {errorMessage}");
					statusLabel.Text = errorMessage;
				}
				else
				{
					statusLabel.Text = "Erreur sans message";
				}
			}
			else
			{
				statusLabel.Text = $"Erreur HTTP {responseCode}";
			}
			statusLabel.Modulate = Colors.Red;
		}
	}
	
	private void CreateUserProfile(string userId, string username, string email)
	{
		currentRequestType = "create_profile";
		string url = $"https://firestore.googleapis.com/v1/projects/{FIREBASE_PROJECT_ID}/databases/(default)/documents/users/{userId}";
		
		var userData = new Godot.Collections.Dictionary
		{
			["fields"] = new Godot.Collections.Dictionary
			{
				["profile"] = new Godot.Collections.Dictionary
				{
					["mapValue"] = new Godot.Collections.Dictionary
					{
						["fields"] = new Godot.Collections.Dictionary
						{
							["username"] = new Godot.Collections.Dictionary { ["stringValue"] = username },
							["email"] = new Godot.Collections.Dictionary { ["stringValue"] = email },
							["createdAt"] = new Godot.Collections.Dictionary { ["timestampValue"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
						}
					}
				},
				["gameData"] = new Godot.Collections.Dictionary
				{
					["mapValue"] = new Godot.Collections.Dictionary
					{
						["fields"] = new Godot.Collections.Dictionary
						{
							["level"] = new Godot.Collections.Dictionary { ["integerValue"] = "1" },
							["experience"] = new Godot.Collections.Dictionary { ["integerValue"] = "0" },
							["stats"] = new Godot.Collections.Dictionary
							{
								["mapValue"] = new Godot.Collections.Dictionary
								{
									["fields"] = new Godot.Collections.Dictionary
									{
										["health"] = new Godot.Collections.Dictionary { ["integerValue"] = "100" },
										["energy"] = new Godot.Collections.Dictionary { ["integerValue"] = "50" },
										["strength"] = new Godot.Collections.Dictionary { ["integerValue"] = "10" },
										["defense"] = new Godot.Collections.Dictionary { ["integerValue"] = "8" },
										["speed"] = new Godot.Collections.Dictionary { ["integerValue"] = "12" }
									}
								}
							}
						}
					}
				}
			}
		};
		
		string jsonString = Json.Stringify(userData);
		string[] headers = { 
			"Content-Type: application/json",
			$"Authorization: Bearer {currentAuthToken}"
		};
		
		httpRequest.Request(url, headers, HttpClient.Method.Patch, jsonString);
	}
	
	private void LoadUserProfile(string userId)
	{
		GD.Print($"Chargement du profil utilisateur: {userId}");
		statusLabel.Text = "Profil chargé avec succès";
		statusLabel.Modulate = Colors.Green;
	}
	
	private string GetFriendlyErrorMessage(string errorMessage)
	{
		return errorMessage switch
		{
			"EMAIL_EXISTS" => "Cette adresse email est déjà utilisée",
			"OPERATION_NOT_ALLOWED" => "L'inscription par email est désactivée",
			"TOO_MANY_ATTEMPTS_TRY_LATER" => "Trop de tentatives. Réessayez plus tard",
			"EMAIL_NOT_FOUND" => "Aucun compte trouvé avec cette adresse email",
			"INVALID_PASSWORD" => "Mot de passe incorrect",
			"USER_DISABLED" => "Ce compte a été désactivé",
			"INVALID_EMAIL" => "Adresse email invalide",
			"WEAK_PASSWORD" => "Le mot de passe est trop faible",
			_ => "Erreur de connexion. Vérifiez vos informations." 
		};
	}
	
	private void SetUIEnabled(bool enabled)
	{
		loginButton.Disabled = !enabled;
		toggleModeButton.Disabled = !enabled;
		emailInput.Editable = enabled;
		passwordInput.Editable = enabled;
		usernameInput.Editable = enabled;
	}
}
