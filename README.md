This plugin gives a purpose to the currently unused blood bags within the game by making them craftable and consumable. Upon consumption, blood bags can heal a portion of your health, stop bleeding, reduce radiation poisoning, boost calorie and hydration, as well as regulate the player's temperature.

[Demonstration](https://youtu.be/27OlLyaih0Q)

-----------------------

## How to Consume
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
  "Version": "2.0.0",
  "Health Increase": 20.0,
  "Calorie Boost": 100.0,
  "Hydration Boost": 50.0,
  "Stop Bleeding": true,
  "Temperature Target": 25.0,
  "Radiation Poisoning Reduction": 25.0,
  "Amount To Consume": 2,
  "Crafting": {
    "Command": "craftblood",
    "Workbench Level Required": 1.0,
    "Crafting Time Seconds": 10.0,
    "Crafting Amount": 0,
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
  "NotEnoughIngredient": "You do not have enough <color=#FFD700>{0}</color>. Required: <color=#FFD700>{0}</color>.",
  "CraftingStart": "Crafting blood bag... Please wait <color=#FFD700>{0}</color> seconds.",
  "NotEnoughBloodBags": "Not enough blood bags. Required: <color=#FFD700>{0}</color>."
}
```

--------

## Credits
 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Default**, up to version 1.8.0
