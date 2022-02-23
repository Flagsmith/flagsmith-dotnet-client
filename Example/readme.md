# Flagsmith Basic DOTNET Example

This directory contains a basic DOTNET application which utilises Flagsmith. To run the example application, you'll 
need to go through the following steps:

1. Create an account, organisation and project on [Flagsmith](https://flagsmith.com)
2. Create a feature in the project called "secret_button"
3. Give the feature a value using the json editor as follows: 

```json
{"colour": "#ababab"}
```

4. Create a appsettings.json file as per given template , update the EnvironmentKey with yours.  
in flagsmith (This can be found on the 'settings' page accessed from the menu on the left under the chosen environment.)
6. Run the app using `dotnet run`
7. Browse to http://localhost:5239

Now you can play around with the 'secret_button' feature in flagsmith, turn it on to show it and edit the colour in the
json value to edit the colour of the button. You can also identify as a given user and then update the settings for the
secret button feature for that user in the flagsmith interface to see the affect that has too. 
