# LeoEcsLite - Легковесный C# Entity Component System фреймворк
Производительность, нулевые или минимальные аллокации, минимизация использования памяти, отсутствие зависимостей от любого игрового движка - это основные цели данного фреймворка.

> **ВАЖНО!** Не забывайте использовать `DEBUG`-версии билдов для разработки и `RELEASE`-версии билдов для релизов: все внутренние проверки/исключения будут работать только в `DEBUG`-версиях и удалены для увеличения производительности в `RELEASE`-версиях.

> **ВАЖНО!** LeoEcsLite-фрейморк **не потокобезопасен** и никогда не будет таким! Если вам нужна многопоточность - вы должны реализовать ее самостоятельно и интегрировать синхронизацию в виде ecs-системы.

# Содержание
* [Социальные ресурсы](#Социальные-ресурсы)
* [Установка](#Установка)
    * [В виде unity модуля](#В-виде-unity-модуля)
    * [В виде исходников](#В-виде-исходников)
    * [Прочие источники](#Прочие-источники)
* [Основные типы](#Основные-типы)
    * [Сущность](#Сущность)
    * [Компонент](#Компонент)
    * [Система](#Система)
* [Совместное использование данных](#Совместное-использование-данных)
* [Специальные типы](#Специальные-типы)
    * [EcsPool](#EcsPool)
    * [EcsFilter](#EcsFilter)
    * [EcsWorld](#EcsWorld)
    * [EcsSystems](#EcsSystems)
* [Интеграция с движками](#Интеграция-с-движками)
    * [Unity](#Unity)
    * [Кастомный движок](#Кастомный-движок)
* [Статьи](#Статьи)
* [Проекты на LeoECS Lite](#Проекты-на-LeoECS-Lite)
    * [С исходниками](#С-исходниками)
    * [Без исходников](#Без-исходников)
* [Расширения](#Расширения)
* [Лицензия](#Лицензия)
* [ЧаВо](#ЧаВо)

# Социальные ресурсы
[![discord](https://img.shields.io/discord/404358247621853185.svg?label=enter%20to%20discord%20server&style=for-the-badge&logo=discord)](https://discord.gg/UQjdcbcHSf)

# Установка

## В виде unity модуля
Поддерживается установка в виде unity-модуля через git-ссылку в PackageManager или прямое редактирование `Packages/manifest.json`:
```
"com.leopotam.ecslite": "https://github.com/Leopotam/ecslite.git",
```
По умолчанию используется последняя релизная версия. Если требуется версия "в разработке" с актуальными изменениями - следует переключиться на ветку `develop`:
```
"com.leopotam.ecslite": "https://github.com/Leopotam/ecslite.git#develop",
```

## В виде исходников
Код так же может быть склонирован или получен в виде архива со страницы релизов.

## Прочие источники
Официальная работоспособная версия размещена по адресу [https://github.com/Leopotam/ecslite](https://github.com/Leopotam/ecslite), все остальные версии (включая *nuget*, *npm* и прочие репозитории) являются неофициальными клонами или сторонним кодом с неизвестным содержимым.

> **ВАЖНО!** Использование этих источников не рекомендуется, только на свой страх и риск.

# Основные типы

## Сущность
Сама по себе ничего не значит и не существует, является исключительно контейнером для компонентов. Реализована как `int`:
```c#
// Создаем новую сущность в мире.
int entity = _world.NewEntity ();

// Любая сущность может быть удалена, при этом сначала все компоненты будут автоматически удалены и только потом энтити будет считаться уничтоженной. 
world.DelEntity (entity);

// Компоненты с любой сущности могут быть скопированы на другую. Если исходная или целевая сущность не существует - будет брошено исключение в DEBUG-версии.
world.CopyEntity (srcEntity, dstEntity);
```

> **ВАЖНО!** Сущности не могут существовать без компонентов и будут автоматически уничтожаться при удалении последнего компонента на них.

## Компонент
Является контейнером для данных пользователя и не должен содержать логику (допускаются минимальные хелперы, но не куски основной логики):
```c#
struct Component1 {
    public int Id;
    public string Name;
}
```
Компоненты могут быть добавлены, запрошены или удалены через [компонентные пулы](#ecspool).

## Система
Является контейнером для основной логики для обработки отфильтрованных сущностей. Существует в виде пользовательского класса, реализующего как минимум один из `IEcsInitSystem`, `IEcsDestroySystem`, `IEcsRunSystem` (и прочих поддерживаемых) интерфейсов:
```c#
class UserSystem : IEcsPreInitSystem, IEcsInitSystem, IEcsRunSystem, IEcsPostRunSystem, IEcsDestroySystem, IEcsPostDestroySystem {
    public void PreInit (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Init() и до срабатывания IEcsInitSystem.Init() у всех систем.
    }
    
    public void Init (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Init() и после срабатывания IEcsPreInitSystem.PreInit() у всех систем.
    }
    
    public void Run (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Run().
    }
    
    public void PostRun (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Run() после срабатывания IEcsRunSystem.Run() у всех систем.
    }

    public void Destroy (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Destroy() и до срабатывания IEcsPostDestroySystem.PostDestroy() у всех систем.
    }
    
    public void PostDestroy (IEcsSystems systems) {
        // Будет вызван один раз в момент работы IEcsSystems.Destroy() и после срабатывания IEcsDestroySystem.Destroy() у всех систем.
    }
}
```

# Совместное использование данных
Экземпляр любого кастомного типа (класса) может быть одновременно подключен ко всем системам:
```c#
class SharedData {
    public string PrefabsPath;
}
...
SharedData sharedData = new SharedData { PrefabsPath = "Items/{0}" };
IEcsSystems systems = new EcsSystems (world, sharedData);
systems
    .Add (new TestSystem1 ())
    .Init ();
...
class TestSystem1 : IEcsInitSystem {
    public void Init(IEcsSystems systems) {
        SharedData shared = systems.GetShared<SharedData> (); 
        string prefabPath = string.Format (shared.PrefabsPath, 123);
        // prefabPath = "Items/123" к этому моменту.
    } 
}
```

# Специальные типы

## EcsPool
Является контейнером для компонентов, предоставляет апи для добавления / запроса / удаления компонентов на сущности:
```c#
int entity = world.NewEntity ();
EcsPool<Component1> pool = world.GetPool<Component1> (); 

// Add() добавляет компонент к сущности. Если компонент уже существует - будет брошено исключение в DEBUG-версии.
ref Component1 c1 = ref pool.Add (entity);

// Has() проверяет наличие компонента на сущности.
bool c1Exists = pool.Has (entity);

// Get() возвращает существующий на сущности компонент. Если компонент не существует - будет брошено исключение в DEBUG-версии.
ref Component1 c1 = ref pool.Get (entity);

// Del() удаляет компонент с сущности. Если компонента не было - никаких ошибок не будет. Если это был последний компонент - сущность будет удалена автоматически.
pool.Del (entity);

// Copy() выполняет копирование всех компонентов с одной сущности на другую. Если исходная или целевая сущность не существует - будет брошено исключение в DEBUG-версии.
pool.Copy (srcEntity, dstEntity);

```

> **ВАЖНО!** После удаления, компонент будет помещен в пул для последующего переиспользования. Все поля компонента будут сброшены в значения по умолчанию автоматически.

## EcsFilter
Является контейнером для хранения отфильтрованных сущностей по наличию или отсутствию определенных компонентов:
```c#
class WeaponSystem : IEcsInitSystem, IEcsRunSystem {
    EcsFilter _filter;
    EcsPool<Weapon> _weapons;
    
    public void Init (IEcsSystems systems) {
        // Получаем экземпляр мира по умолчанию.
        EcsWorld world = systems.GetWorld ();
        
        // Мы хотим получить все сущности с компонентом "Weapon" и без компонента "Health".
        // Фильтр хранит только сущности, сами даные лежат в пуле компонентов "Weapon".
        // Фильтр может собираться динамически каждый раз, но рекомендуется кеширование.
        _filter = world.Filter<Weapon> ().Exc<Health> ().End ();
        
        // Запросим и закешируем пул компонентов "Weapon".
        _weapons = world.GetPool<Weapon> ();
        
        // Создаем новую сущность для теста.
        int entity = world.NewEntity ();
        
        // И добавляем к ней компонент "Weapon" - эта сущность должна попасть в фильтр.
        _weapons.Add (entity);
    }

    public void Run (IEcsSystems systems) {
        foreach (int entity in filter) {
            ref Weapon weapon = ref _weapons.Get (entity);
            weapon.Ammo = System.Math.Max (0, weapon.Ammo - 1);
        }
    }
}
```

> **ВАЖНО!** Фильтр достаточно собрать один раз и закешировать, пересборка для обновления списка сущностей не нужна.

Дополнительные требования к отфильтровываемым сущностям могут быть добавлены через методы `Inc<>()` / `Exc<>()`.

> **ВАЖНО!** Фильтры поддерживают любое количество требований к компонентам, но один и тот же компонент не может быть в списках "include" и "exclude".

## EcsWorld
Является контейнером для всех сущностей, компонентых пулов и фильтров, данные каждого экземпляра уникальны и изолированы от других миров.

> **ВАЖНО!** Необходимо вызывать `EcsWorld.Destroy()` у экземпляра мира если он больше не нужен.

## EcsSystems
Является контейнером для систем, которыми будет обрабатываться `EcsWorld`-экземпляр мира:
```c#
class Startup : MonoBehaviour {
    EcsWorld _world;
    IEcsSystems _systems;

    void Start () {
        // Создаем окружение, подключаем системы.
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world);
        _systems
            .Add (new WeaponSystem ())
            .Init ();
    }
    
    void Update () {
        // Выполняем все подключенные системы.
        _systems?.Run ();
    }

    void OnDestroy () {
        // Уничтожаем подключенные системы.
        if (_systems != null) {
            _systems.Destroy ();
            _systems = null;
        }
        // Очищаем окружение.
        if (_world != null) {
            _world.Destroy ();
            _world = null;
        }
    }
}
```

> **ВАЖНО!** Необходимо вызывать `IEcsSystems.Destroy()` у экземпляра группы систем если он больше не нужен.

# Интеграция с движками

## Unity
> Проверено на Unity 2020.3 (не зависит от нее) и содержит asmdef-описания для компиляции в виде отдельных сборок и уменьшения времени рекомпиляции основного проекта.

[Интеграция в Unity editor](https://github.com/Leopotam/ecslite-unityeditor) содержит шаблоны кода, а так же предоставляет мониторинг состояния мира.

## Кастомный движок
> Для использования фреймворка требуется C#7.3 или выше.

Каждая часть примера ниже должна быть корректно интегрирована в правильное место выполнения кода движком:
```c#
using Leopotam.EcsLite;

class EcsStartup {
    EcsWorld _world;
    IEcsSystems _systems;

    // Инициализация окружения.
    void Init () {        
        _world = new EcsWorld ();
        _systems = new EcsSystems (_world);
        _systems
            // Дополнительные экземпляры миров
            // должны быть зарегистрированы здесь.
            // .AddWorld (customWorldInstance, "events")
            
            // Системы с основной логикой должны
            // быть зарегистрированы здесь.
            // .Add (new TestSystem1 ())
            // .Add (new TestSystem2 ())
            
            .Init ();
    }

    // Метод должен быть вызван из
    // основного update-цикла движка.
    void UpdateLoop () {
        _systems?.Run ();
    }

    // Очистка окружения.
    void Destroy () {
        if (_systems != null) {
            _systems.Destroy ();
            _systems = null;
        }
        if (_world != null) {
            _world.Destroy ();
            _world = null;
        }
    }
}
```

# Статьи

* ["Создание dungeon crawler'а с LeoECS Lite. Часть 1"](https://habr.com/ru/post/661085/)

  [![](https://habrastorage.org/r/w1560/getpro/habr/upload_files/372/b1c/ad3/372b1cad308788dac56f8db1ea16b9c9.png)](https://habr.com/ru/post/661085/)

* ["Создание dungeon crawler'а с LeoECS Lite. Часть 2"](https://habr.com/ru/post/673926/)

  [![](https://habrastorage.org/r/w1560/getpro/habr/upload_files/63f/3ef/c47/63f3efc473664fdaaf1a249f258e2486.png)](https://habr.com/ru/post/673926/)

# Проекты на LeoECS Lite
## С исходниками

* ["SlimeHunter"](https://github.com/JimboA/SlimeHunter-LeoEcsLite)
  
  [![](https://media.githubusercontent.com/media/JimboA/SlimeHunter-LeoEcsLite/main/Screenshot_1.png)](https://github.com/JimboA/SlimeHunter-LeoEcsLite)


* ["3D Platformer"](https://github.com/supremestranger/3D-Platformer-Lite)

  [![](https://camo.githubusercontent.com/dcd2f525130d73f4688c1f1cfb12f6e37d166dae23a1c6fac70e5b7873c3ab21/68747470733a2f2f692e6962622e636f2f686d374c726d342f506c6174666f726d65722e706e67)](https://github.com/supremestranger/3D-Platformer-Lite)


* ["SharpPhysics2D"](https://github.com/7Bpencil/sharpPhysics)

  [![](https://github.com/7Bpencil/sharpPhysics/raw/master/pictures/preview.png)](https://github.com/7Bpencil/sharpPhysics)

## Без исходников
* ["Супер Био-Мужик" (STEAM)](https://store.steampowered.com/app/2144580/Super_BioMan/)

  [![](https://cdn.akamai.steamstatic.com/steam/apps/2144580/header.jpg)](https://cdn.akamai.steamstatic.com/steam/apps/2144580/header.jpg)


* ["Microbiome" (WIP)](https://vk.com/microbiomegame)

  [![](https://img.youtube.com/vi/WTciasBN2eQ/0.jpg)](https://www.youtube.com/watch?v=WTciasBN2eQ)

# Расширения
* [Инъекция зависимостей](https://github.com/Leopotam/ecslite-di)
* [Расширенные системы](https://github.com/Leopotam/ecslite-extendedsystems)
* [Поддержка многопоточности](https://github.com/Leopotam/ecslite-threads)
* [Интеграция в редактор Unity](https://github.com/Leopotam/ecslite-unityeditor)
* [Поддержка Unity uGui](https://github.com/Leopotam/ecslite-unity-ugui)
* [Unity Physx events support](https://github.com/supremestranger/leoecs-lite-physics)
* [Multiple Shared injection](https://github.com/GoodCatGames/ecslite-multiple-shared)
* [EasyEvents](https://github.com/7Bpencil/ecslite-easyevents)
* [Entity command buffer](https://github.com/JimboA/EcsLiteEntityCommandBuffer)
* [Интеграция в редактор Unity на базе UIToolkit](https://github.com/Mitfart/LeoECSLite.UnityIntegration)
* [Unity Entity Converter](https://github.com/AndreyBirchenko/LeoEcsLiteEntityConverter)
* [Interval Systems](https://github.com/nenuacho/ecslite-interval-systems)
* [Quadtree Systems](https://github.com/nenuacho/ecslite-quadtree)
* [LeoECS Lite Unity Zoo](https://github.com/aleverdes/leoecslite-zoo)
* [Adding/removing components debugger for LeoECS Lite](https://github.com/supremestranger/LiteEzDebuggerModule)

# Лицензия
Фреймворк выпускается под двумя лицензиями, [подробности тут](./LICENSE.md).

В случаях лицензирования по условиям MIT-Red не стоит расчитывать на
персональные консультации или какие-либо гарантии.

# ЧаВо

### В чем отличие от старой версии LeoECS?

Я предпочитаю называть их `лайт` (ecs-lite) и `классика` (leoecs). Основные отличия `лайта` следующие:
* Кодовая база фреймворка уменьшилась в 2 раза, ее стало проще поддерживать и расширять.
* `Лайт` не является порезанной версией `классики`, весь функционал сохранен в виде ядра и внешних модулей.
* Отсутствие каких-либо статичных данных в ядре.
* Отсутствие кешей компонентов в фильтрах, это уменьшает потребление памяти и увеличивает скорость перекладывания сущностей по фильтрам.
* Быстрый доступ к любому компоненту на любой сущности (а не только отфильтрованной и через кеш фильтра).
* Нет ограничений на количество требований/ограничений к компонентам для фильтров.
* Общая линейная производительность близка к `классике`, но доступ к компонентам, перекладывание сущностей по фильтрам стал несоизмеримо быстрее.
* Прицел на использование мультимиров - нескольких экземпляров миров одновременно с разделением по ним данных для оптимизации потребления памяти.
* Отсутствие рефлексии в ядре, возможно использование агрессивного вырезания неиспользуемого кода компилятором (code stripping, dead code elimination).
* Совместное использование общих данных между системами происходит без рефлексии (если она допускается, то рекомендуется использовать расширение `ecslite-di` из списка расширений).
* Реализация сущностей вернулась к обычныму типу `int`, это сократило потребление памяти. Если сущности нужно сохранять где-то - их по-прежнему нужно упаковывать в специальную структуру.
* Маленькое ядро, весь дополнительный функционал реализуется через подключение опциональных расширений.
* Весь новый функционал будет выходить только к `лайт`-версии, `классика` переведена в режим поддержки на исправление ошибок.

### Я хочу одну систему вызвать в `MonoBehaviour.Update()`, а другую - в `MonoBehaviour.FixedUpdate()`. Как я могу это сделать?

Для разделения систем на основе разных методов из `MonoBehaviour` необходимо создать под каждый метод отдельную `IEcsSystems`-группу:
```c#
IEcsSystems _update;
IEcsSystems _fixedUpdate;

void Start () {
    EcsWorld world = new EcsWorld ();
    _update = new EcsSystems (world);
    _update
        .Add (new UpdateSystem ())
        .Init ();
    _fixedUpdate = new EcsSystems (world);
    _fixedUpdate
        .Add (new FixedUpdateSystem ())
        .Init ();
}

void Update () {
    _update?.Run ();
}

void FixedUpdate () {
    _fixedUpdate?.Run ();
}
```

### Меня не устраивают значения по умолчанию для полей компонентов. Как я могу это настроить?

Компоненты поддерживают установку произвольных значений через реализацию интерфейса `IEcsAutoReset<>`:
```c#
struct MyComponent : IEcsAutoReset<MyComponent> {
    public int Id;
    public object SomeExternalData;

    public void AutoReset (ref MyComponent c) {
        c.Id = 2;
        c.SomeExternalData = null;
    }
}
```
Этот метод будет автоматически вызываться для всех новых компонентов, а так же для всех только что удаленных, до помещения их в пул.
> **ВАЖНО!** В случае применения `IEcsAutoReset` все дополнительные очистки/проверки полей компонента отключаются, что может привести к утечкам памяти. Ответственность лежит на пользователе!

### Меня не устраивают значения для полей компонентов при их копировании через EcsWorld.CopyEntity() или Pool<>.Copy(). Как я могу это настроить?

Компоненты поддерживают установку произвольных значений при вызове `EcsWorld.CopyEntity()` или `EcsPool<>.Copy()` через реализацию интерфейса `IEcsAutoCopy<>`:
```c#
struct MyComponent : IEcsAutoCopy<MyComponent> {
    public int Id;

    public void AutoCopy (ref MyComponent src, ref MyComponent dst) {
        dst.Id = src.Id * 123;
    }
}
```
> **ВАЖНО!** В случае применения `IEcsAutoCopy` никакого копирования по умолчанию не происходит. Ответственность за корректность заполнения данных и за целостность исходных лежит на пользователе!

### Я хочу сохранить ссылку на сущность в компоненте. Как я могу это сделать?

Для сохранения ссылки на сущность ее необходимо упаковать в один из специальных контейнеров (`EcsPackedEntity` или `EcsPackedEntityWithWorld`):
```c#
EcsWorld world = new EcsWorld ();
int entity = world.NewEntity ();
EcsPackedEntity packed = world.PackEntity (entity);
EcsPackedEntityWithWorld packedWithWorld = world.PackEntityWithWorld (entity);
...
// В момент распаковки мы проверяем - жива эта сущность или уже нет.
if (packed.Unpack (world, out int unpacked)) {
    // "unpacked" является валидной сущностью и мы можем ее использовать.
}

// В момент распаковки мы проверяем - жива эта сущность или уже нет.
if (packedWithWorld.Unpack (out EcsWorld unpackedWorld, out int unpackedWithWorld)) {
    // "unpackedWithWorld" является валидной сущностью и мы можем ее использовать.
}
```

### Я хочу добавить реактивности и обрабатывать события изменений в мире самостоятельно. Как я могу сделать это?

> **ВАЖНО!** Так делать не рекомендуется из-за падения производительности.

Для активации этого функционала следует добавить `LEOECSLITE_WORLD_EVENTS` в список директив комплятора, а затем - добавить слушатель событий:

```c#
class TestWorldEventListener : IEcsWorldEventListener {
    public void OnEntityCreated (int entity) {
        // Сущность создана - метод будет вызван в момент вызова world.NewEntity().
    }

    public void OnEntityChanged (int entity) {
        // Сущность изменена - метод будет вызван в момент вызова pool.Add() / pool.Del().
    }

    public void OnEntityDestroyed (int entity) {
        // Сущность уничтожена - метод будет вызван в момент вызова world.DelEntity() или в момент удаления последнего компонента.
    }

    public void OnFilterCreated (EcsFilter filter) {
        // Фильтр создан - метод будет вызван в момент вызова world.Filter().End(), если фильтр не существовал ранее.
    }

    public void OnWorldResized (int newSize) {
        // Мир изменил размеры - метод будет вызван в случае изменения размеров кешей под сущности в момент вызова world.NewEntity().
    }

    public void OnWorldDestroyed (EcsWorld world) {
        // Мир уничтожен - метод будет вызван в момент вызова world.Destroy().
    }
}
...
var world = new EcsWorld ();
var listener = new TestWorldEventListener ();
world.AddEventListener (listener);
``` 

### Я хочу добавить реактивщины и обрабатывать события изменения фильтров. Как я могу это сделать?

> **ВАЖНО!** Так делать не рекомендуется из-за падения производительности.

Для активации этого функционала следует добавить `LEOECSLITE_FILTER_EVENTS` в список директив комплятора, а затем - добавить слушатель событий:

```c#
class TestFilterEventListener : IEcsFilterEventListener {
    public void OnEntityAdded (int entity) {
        // Сущность добавлена в фильтр.
    }

    public void OnEntityRemoved (int entity) {
        // Сущность удалена из фильтра.
    }
}
...
var world = new EcsWorld ();
var filter = world.Filter<C1> ().End ();
var listener = new TestFilterEventListener ();
filter.AddEventListener (listener);
``` 