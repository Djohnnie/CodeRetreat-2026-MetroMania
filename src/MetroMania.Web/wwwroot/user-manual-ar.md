# دليل لاعب مترو مانيا

## مرحباً بك في التحدي

**مترو مانيا** هو تحدي برمجي تكتب فيه روبوتاً بلغة C# لإدارة شبكة مترو متنامية. يستمع روبوتك إلى أحداث اللعبة، ويخطط المسارات، ويصدر أمراً واحداً كل ساعة للحفاظ على تدفق الركاب في المدينة. تنتهي اللعبة عندما يتراكم عدد كبير جداً من الركاب في محطة واحدة — أبقِ شبكتك في حركة دائمة!

## عالم اللعبة

تجري المحاكاة على شبكة مربعات. مع تقدم اللعبة، تظهر محطات جديدة في مواقع مختلفة على الخريطة. قد تملأ مربعات المياه أجزاء من الشبكة، لكنها لا تحجب خطوط المترو — إذ تسير الخطوط فوق التضاريس من الناحية المفاهيمية.

### أنواع المحطات

لكل محطة **شكل** مميز. ينشأ الركاب في المحطات ويريدون الوصول إلى *أي* محطة تتطابق مع شكلهم. هناك ستة أنواع:

| الشكل | الاسم |
|-------|-------|
| ● | دائرة |
| ■ | مستطيل |
| ▲ | مثلث |
| ◆ | معين |
| ⬠ | خماسي |
| ✦ | نجمة |

## الركاب

يظهر الركاب في المحطات بمرور الوقت وفقاً لجدول ظهور كل محطة. هدف الراكب هو الوصول إلى **أي** محطة من النوع الصحيح — وليس محطة بعينها.

الركاب أذكياء: يركبون القطار فقط إذا كان خطه يقود فعلاً نحو محطة من نوع وجهتهم. الخطوط المسدودة لن تجذب الركاب.

> **تحذير:** عندما تصل محطة إلى **10 ركاب أو أكثر** في الانتظار، يُستدعى `OnStationOverrun`. تصرف بسرعة!

> **انتهت اللعبة:** عندما تصل أي محطة إلى **20 راكباً أو أكثر** في الانتظار، تنتهي اللعبة فوراً.

## الموارد

تبدأ كل جولة بمجموعة محدودة من الموارد:

- **خط واحد** — يُعرِّف مسار مترو بين المحطات
- **قطار واحد** — مركبة لوضعها على خط

كل **اثنين عند منتصف الليل** (بداية كل أسبوع في اللعبة)، يتلقى روبوتك **هدية أسبوعية**: مورد إضافي واحد يُختار عشوائياً — إما **خط** أو **قطار** أو **عربة**.

| المورد | الغرض |
|--------|--------|
| خط | تعريف مسار يربط محطتين أو أكثر |
| قطار | مركبة تسير ذهاباً وإياباً على الخط |
| عربة | تُوصل بقطار لزيادة سعته بمقدار +1 |

يحمل كل قطار **6 ركاب** افتراضياً. تزيد إضافة العربات هذه السعة بمقدار 1 لكل عربة.

## الوقت

تتقدم المحاكاة **ساعة بساعة** (24 ساعة = يوم واحد). كل استدعاء لـ `OnHourTick` يمثل ساعة لعب واحدة. تعمل المرحلة عادةً حتى 200 يوم أو حتى وقوع حدث انتهاء اللعبة.

## روبوتك — واجهة IMetroManiaRunner

روبوتك كلاس C# يُطبِّق `IMetroManiaRunner`. تستدعي محرك اللعبة هذه الطرق عند وقوع الأحداث خلال المحاكاة:

### استدعاءات الأحداث

| الطريقة | تُستدعى عند | تُعيد |
|---------|------------|-------|
| `OnHourTick(snapshot)` | كل ساعة | `PlayerAction` |
| `OnDayStart(snapshot)` | منتصف الليل كل يوم | — |
| `OnWeeklyGift(snapshot, gift)` | كل اثنين منتصف الليل | — |
| `OnStationSpawned(snapshot, id, location, type)` | ظهور محطة جديدة | — |
| `OnPassengerWaiting(snapshot, location, passengers)` | بدء انتظار راكب جديد | — |
| `OnStationOverrun(snapshot, location, passengers)` | 10+ ركاب في محطة | — |
| `OnGameOver(snapshot, location, passengers)` | 20+ ركاب — اللعبة تنتهي | — |

> **`OnHourTick` هو حلقة التحكم الرئيسية.** إنها الاستدعاء الوحيد الذي يُعيد إجراءً. جميع الاستدعاءات الأخرى إعلامية فقط — استخدمها لتحديث حالتك والتخطيط المسبق.

### GameSnapshot

يتلقى كل استدعاء `GameSnapshot` يحتوي على الحالة الكاملة الراهنة:

- `snapshot.Stations` — جميع المحطات (المعرف، الموقع، النوع، عدد الركاب)
- `snapshot.Lines` — جميع الخطوط النشطة (المعرف، قائمة مرتبة بمعرّفات المحطات)
- `snapshot.Vehicles` — جميع المركبات (المعرف، الخط، الركاب على متنها، الموقع)
- `snapshot.Resources.AvailableLines` — موارد الخطوط غير الموضوعة
- `snapshot.Resources.AvailableVehicles` — القطارات غير الموضوعة
- `snapshot.Resources.AvailableWagons` — العربات غير المُرفقة
- `snapshot.GameTime` — `اليوم` و`الساعة` الحاليان

---

## إجراءات اللاعب

من `OnHourTick`، أعِد **إجراءً واحداً** فقط لكل نبضة. يمكنك تنفيذ عملية واحدة فقط في الساعة — اختر بحكمة.

### `CreateLine` — إنشاء مسار جديد

```csharp
return new CreateLine(
    LineId: Guid.NewGuid(),
    StationIds: [stationA, stationB]);
```

ينشئ خط مترو جديد يربط محطتين أو أكثر بالترتيب. يستهلك مورد **خط** واحد متاح.

### `ExtendLine` — إضافة محطة إلى خط

```csharp
return new ExtendLine(
    LineId: existingLineId,
    FromStationId: currentEndStation,
    ToStationId: newStation);
```

يضيف محطة إلى بداية أو نهاية خط موجود. يجب أن يكون `FromStationId` أول أو آخر محطة في الخط.

### `InsertStationInLine` — إدراج محطة في المنتصف

```csharp
return new InsertStationInLine(
    LineId: existingLineId,
    NewStationId: stationToInsert,
    FromStationId: adjacentStationA,
    ToStationId: adjacentStationB);
```

يُدرج محطة جديدة بين محطتين متجاورتين بالفعل على الخط.

### `RemoveLine` — حذف مسار

```csharp
return new RemoveLine(LineId: lineId);
```

يحذف خطاً كاملاً ويُعيد جميع موارده (رمز الخط + جميع المركبات عليه) إلى مجموعتك.

### `AddVehicleToLine` — نشر قطار

```csharp
return new AddVehicleToLine(
    VehicleId: availableVehicleId,
    LineId: targetLineId,
    StationId: startingStation);
```

يضع قطاراً متاحاً على خط من محطة البداية. يبدأ القطار في التنقل فوراً.

### `RemoveVehicle` — سحب قطار

```csharp
return new RemoveVehicle(VehicleId: vehicleId);
```

يُزيل مركبة من خطها ويُعيدها إلى مجموعتك المتاحة.

### `AddWagonToTrain` — زيادة السعة

```csharp
return new AddWagonToTrain(
    WagonId: availableWagonId,
    TrainId: targetTrainId);
```

يُرفق عربة بقطار موضوع على خط. كل عربة تُضيف **+1 سعة ركاب**.

### `MoveWagonBetweenTrains` — إعادة توزيع السعة

```csharp
return new MoveWagonBetweenTrains(
    WagonId: wagonId,
    SourceTrainId: fromTrain,
    DestinationTrainId: toTrain);
```

ينقل عربة مُرفقة من قطار نشط إلى آخر. يجب أن يكون كلا القطارين منشورَين على خطوط.

### `NoAction` — تخطي هذه النبضة

```csharp
return PlayerAction.None;  // أو: return new NoAction();
```

---

## الكود الابتدائي

تبدأ مشاركتك من هذا القالب:

```csharp
public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameSnapshot snapshot) => PlayerAction.None;

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGift(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId,
        Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }
}
```

---

## النقاط

تزداد نقاطك في كل مرة يصل فيها راكب بنجاح إلى محطة من نوع وجهته. تعمل كل مرحلة لعدد محدد من الأيام. كلما **طالت مدة بقائك** وكلما **زاد عدد الركاب الذين توصلهم**، كانت نقاطك أعلى. **أفضل نتيجة لك في كل مرحلة** هي ما يُحسب في لوحة المتصدرين.

---

## نصائح استراتيجية

- **احفظ معرّفات المحطات في `OnStationSpawned`** — خزّنها في حقول الكلاس للتصرف بشأنها في `OnHourTick` التالية.
- **استجب فوراً لـ `OnStationOverrun`** — عند 10 ركاب لديك حوالي 10 ساعات قبل انتهاء اللعبة.
- **اربط كل محطة** — المحطة غير المتصلة تتراكم فيها الركاب بلا مفر. أعطِ دائماً الأولوية للمحطات الجديدة.
- **فكّر في طوبولوجية الخط** — شبكات المحور أو الحلقة عادةً أفضل من سلسلة طويلة واحدة.
- **أضف عربات للخطوط المزدحمة** — تزيد العربات من إنتاجية الخطوط ذات الطلب العالي بشكل ملحوظ.
- **الهدايا الأسبوعية ثمينة** — استخدم الموارد الجديدة في أقرب وقت ممكن بعد استلامها.
- **توجيه الركاب ذكي** — يركب الركاب القطار فقط إذا كان خطه يقود نحو وجهتهم. ربط المحطات عشوائياً لا يفيد.
- **احذف وأعِد البناء** — هدم خط سيء وإعادة بنائه بمسار أفضل يستحق أحياناً الاضطراب المؤقت.
