# Isometric Map Generator

I was working on this generator on and off for a long time. For [Proc Jam 2018](http://www.procjam.com/) I decided to work on it again and finally release it.

The map generator is inspired by my interest in procedural generation as a whole and the aesthetics of 90's isometric tycoon games like RollerCoaster Tycoon or Transport Tycoon.

The project uses [Monogame](http://www.monogame.net/) and depends on my own "game engine" library that is currently not publicly available, but I'm working on releasing it.

You can find a downloadable release on [itch.io](https://meursault.itch.io/isometric-map-generator).

## What is (not) done

- [x] Terrain generation
- [x] City generation
- [x] Usability improvements (movement, zoom, etc)
- [x] Reproducible random generation by a seed
- [ ] Bug fixes
- [ ] More diverse landscapes, especially mountains
- [ ] More efficient and better rivers
- [ ] More varied city sizes
- [ ] Better roads between cities
- [ ] Better forests
- [ ] Generating city names
- [ ] Graphics improvement
  - [ ] Bridges
  - [ ] More trees
  - [ ] Animated water
  - [ ] Dynamic lighting

## Project Structure

The map generation is separated into modules. The modules work on a 2D-array of tiles and manipulate their type and other parameters as needed. If you are interested in the generation checkout [generation folder](World/Generation).