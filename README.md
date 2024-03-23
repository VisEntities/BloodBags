This plugin gives a purpose to the currently unused blood bags within the game by making them craftable and consumable. Upon consumption, blood bags can heal a portion of your health, stop bleeding, reduce radiation poisoning, boost calorie and hydration, as well as regulate the player's temperature.

[Demonstration](https://youtu.be/27OlLyaih0Q)

-----------------------

## Crafting

Once you have all the necessary ingredients in your inventory and are near the workbench of the required level, use the command specified in the config to start crafting. Be aware that crafting blood bags will sacrifice a configurable amount of your health, so make sure you have sufficient health before crafting.

---------------------

## Consuming
Blood bags must be placed in your toolbar inventory to be consumed. Then select them and click the use button.

---------------------

## Permissions
- `bloodbags.use` - Allows to craft and consume blood bags.

-----------------

## Chat Commands
- `craftblood` - Begins the crafting of the blood bag.

-------------

## Configuration
```json
{
  "Version": "2.3.0",
  "Instant Health Increase": 20.0,
  "Health Increase Over Time": 20.0,
  "Calorie Boost": 100.0,
  "Hydration Boost": 50.0,
  "Stop Bleeding": true,
  "Temperature Target": 25.0,
  "Radiation Poisoning Reduction": 25.0,
  "Amount To Consume": 2,
  "Crafting": {
    "Command": "craftblood",
    "Workbench Level Required": 1.0,
    "Health Sacrifice Amount": 15.0,
    "Crafting Time Seconds": 10.0,
    "Crafting Amount": 2,
    "Ingredients": [
      {
        "Shortname": "cloth",
        "Amount": 20
      },
      {
        "Shortname": "ducttape",
        "Amount": 1
      },
      {
        "Shortname": "skull.human",
        "Amount": 1
      }
    ]
  }
}
```

---------

## Localization

```json
{
  "NoPermission": "You do not have permission to craft this item.",
  "NeedWorkbench": "You need to be near a workbench level <color=#FFD700>{0}</color> to craft this item.",
  "NotEnoughIngredient": "You do not have enough <color=#FFD700>{0}</color>. Required: <color=#FFD700>{1}</color>.",
  "CraftingStart": "Crafting blood bag... Please wait <color=#FFD700>{0}</color> seconds.",
  "NotEnoughBloodBags": "Not enough blood bags. Required: <color=#FFD700>{0}</color>.",
  "InsufficientHealth": "You don't have enough health to craft a blood bag. Required health: <color=#FFD700>{0}</color>.",
  "UseInstruction": "Press <color=#FFD700>use</color> to consume",
  "CraftingCountdown": "Crafting blood bag, <color=#FFD700>{0}</color> seconds remaining"
}
```

--------

## Credits
 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Default**, up to version 1.8.0