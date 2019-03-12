# Isometric Map Generator

I was working on this generator on and off for a long time. For [Proc Jam 2018](http://www.procjam.com/) I decided to finish and release it.

The map generator is inspired by my interest in procedural generation as a whole and the aesthetics of 90's isometric tycoon games like RollerCoaster Tycoon or Transport Tycoon.

The project uses [Monogame](http://www.monogame.net/) and depends on my own "game engine" library that is currently not publicly available, but I'm working on releasing it.

You can find a downloadable release on [itch.io](https://meursault.itch.io/isometric-map-generator).

## What is (not) done

- [x] Terrain generation
- [x] City generation
- [x] Usability improvements (movement, zoom, etc)
- [x] Reproducible random generation by a seed
- [x] Better road connections between cities
- [x] Variable forest size
- [x] More trees
- [x] Animated water
- [x] Better rivers
- [x] City districts
- [x] Diagonal roads
- [ ] Faster rivers
- [ ] Bridges
- [ ] City names
- [ ] Dynamic lighting
- [ ] More content

## Project Structure

The map generation is separated into modules. The modules work on a 2D-array of tiles and manipulate their type and other parameters as needed. If you are interested in the generation checkout [generation folder](World/Generation).