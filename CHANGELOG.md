# [0.1.0-preview.4](https://github.com/tenekon/Tenekon.Coroutines/compare/0.1.0-preview.3...0.1.0-preview.4) (2024-10-06)


### Bug Fixes

* removed compile-only dependencies from package manifest ([5cd9c8d](https://github.com/tenekon/Tenekon.Coroutines/commit/5cd9c8da2d1e25b8c6581ac8815ae6be54b07b00))



# [0.1.0-preview.3](https://github.com/tenekon/Tenekon.Coroutines/compare/0.1.0-preview.2...0.1.0-preview.3) (2024-10-06)


* feat!: renamed YieldReturn to YieldAssign and moved Yielders.Yield to Coroutine.Yield ([49e3369](https://github.com/tenekon/Tenekon.Coroutines/commit/49e3369e802ef5d9603822a032795197fede2d56))


### Bug Fixes

* changed Exchange<T>.Result to changed Exchange<T>.Value ([1776104](https://github.com/tenekon/Tenekon.Coroutines/commit/17761046bac8acaa500af5c02b961c4d628f8f8d))
* fixed a bug where the last false-resulting MoveNextAsync did not wait for background tasks ([e33336e](https://github.com/tenekon/Tenekon.Coroutines/commit/e33336e5099d364816090354ef439290f1fa2e1a))
* small bug fixes ([ab1c098](https://github.com/tenekon/Tenekon.Coroutines/commit/ab1c0987116ebaa2ee9daf537b41da862fc0eb34))


### Features

* implemented Coroutine.Run, equivalent to Task.Run, and Coroutine.Factory.StartNew, equivalent to Task.Factory.StartNew ([a8a43f7](https://github.com/tenekon/Tenekon.Coroutines/commit/a8a43f74816349a2228cdc48f653a0b18cc9b4c6))
* make yielder arguments comparable ([5228103](https://github.com/tenekon/Tenekon.Coroutines/commit/5228103b344c055abf67fbbc70ebd950c6d28447))
* overhauled WithContext ([3366b9a](https://github.com/tenekon/Tenekon.Coroutines/commit/3366b9a4cc544462254e97356c114ca324aa2f7c))


### BREAKING CHANGES

* To better reflect that you "replace" the coroutine result of a yielder the name YieldReturn has been renamed to YieldAssign. Also moved Yielders.Yield to Coroutine.Yield to align with Task.Yield.



# [0.1.0-preview.2](https://github.com/tenekon/Tenekon.Coroutines/compare/0.1.0-preview.1...0.1.0-preview.2) (2024-09-26)


### Features

* added YieldReturn() to answer yielders that do not have return values and allow to overwrite the current yielder of an async iteator ([4ef95c9](https://github.com/tenekon/Tenekon.Coroutines/commit/4ef95c97e38be6ed25ba4b8112a2717c58c7e4bc))



# 0.1.0-preview.1 (2024-09-26)


### Features

* release first preview of Tenekon.Coroutines :tada: ([3de8870](https://github.com/tenekon/Tenekon.Coroutines/commit/3de887067787aa36b4979c3bb9da4c1d9ca01189))



