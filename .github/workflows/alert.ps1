$downloadUrl = "https://github.com/tsunamods-codes/Junction-VIII/releases/latest"

if ($env:_IS_BUILD_CANARY -eq "true") {
  $downloadUrl = "https://github.com/tsunamods-codes/Junction-VIII/releases/tag/canary"
}

# Initial template from https://discohook.org/
$discordPost = @"
{
  "username": "Junction VIII",
  "avatar_url": "https://github.com/tsunamods-codes/Junction-VIII/raw/master/.logo/app.png",
  "content": "Release **${env:_RELEASE_VERSION}** has just been published!\n\nDownload Url: ${downloadUrl}\n\nIf you find something broken or unexpected, feel free to check existing ones first here https://github.com/tsunamods-codes/Junction-VIII/issues.\nIf non existing, then report your issue here https://github.com/tsunamods-codes/Junction-VIII/issues/new.\n\nThank you for using Junction VIII!",
  "embeds": [
    {
      "title": "FAQ",
      "description": "Having issues? Feel free to check this FAQ page: https://forum.tsunamods.com/viewtopic.php?f=16&t=60",
      "color": 7506394
    },
    {
      "title": "Junction VIII is FOSS Software!",
      "description": "Junction VIII is released under MS-PL license. More info here: https://github.com/tsunamods-codes/Junction-VIII#license",
      "color": 15746887
    }
  ]
}
"@

Invoke-RestMethod -Uri $env:_MAP_J8_INTERNAL -ContentType "application/json" -Method Post -Body $discordPost
Invoke-RestMethod -Uri $env:_MAP_J8_QHIMM -ContentType "application/json" -Method Post -Body $discordPost
Invoke-RestMethod -Uri $env:_MAP_J8_TSUNAMODS -ContentType "application/json" -Method Post -Body $discordPost
