enum Tab {
    Sounds = "Sounds",
    Rows = "Rows",
    Actions = "Actions",
    Decorations = "Sprites",
    Rooms = "Rooms",
    Windows = "Windows",
    Unknown = "Unknown",
}
enum SayReadyGetSetGoWords {
    SayReaDyGetSetGoNew = "SayReaDyGetSetGoNew",
    SayReaDyGetSetOne = "SayReaDyGetSetOne",
    SayGetSetGo = "SayGetSetGo",
    SayGetSetOne = "SayGetSetOne",
    JustSayRea = "JustSayRea",
    JustSayDy = "JustSayDy",
    JustSayGet = "JustSayGet",
    JustSaySet = "JustSaySet",
    JustSayAnd = "JustSayAnd",
    JustSayGo = "JustSayGo",
    JustSayStop = "JustSayStop",
    JustSayAndStop = "JustSayAndStop",
    SaySwitch = "SaySwitch",
    SayWatch = "SayWatch",
    SayListen = "SayListen",
    Count1 = "Count1",
    Count2 = "Count2",
    Count3 = "Count3",
    Count4 = "Count4",
    Count5 = "Count5",
    Count6 = "Count6",
    Count7 = "Count7",
    Count8 = "Count8",
    Count9 = "Count9",
    Count10 = "Count10",
    SayReadyGetSetGo = "SayReadyGetSetGo",
    JustSayReady = "JustSayReady",
}
enum EventType {
    AddClassicBeat = "AddClassicBeat",
    AddFreeTimeBeat = "AddFreeTimeBeat",
    AddOneshotBeat = "AddOneshotBeat",
    AdvanceText = "AdvanceText",
    BassDrop = "BassDrop",
    Blend = "Blend",
    CallCustomMethod = "CallCustomMethod",
    ChangeCharacter = "ChangeCharacter",
    ChangePlayersRows = "ChangePlayersRows",
    Comment = "Comment",
    CustomFlash = "CustomFlash",
    DesktopColor = "DesktopColor",
    FadeRoom = "FadeRoom",
    FinishLevel = "FinishLevel",
    Flash = "Flash",
    FlipScreen = "FlipScreen",
    FloatingText = "FloatingText",
    ForwardDecorationEvent = "ForwardDecorationEvent",
    ForwardEvent = "ForwardEvent",
    ForwardRowEvent = "ForwardRowEvent",
    HideRow = "HideRow",
    HideWindow = "HideWindow",
    InvertColors = "InvertColors",
    MacroEvent = "MacroEvent",
    MaskRoom = "MaskRoom",
    Move = "Move",
    MoveCamera = "MoveCamera",
    MoveRoom = "MoveRoom",
    MoveRow = "MoveRow",
    NarrateRowInfo = "NarrateRowInfo",
    NewWindowDance = "NewWindowDance",
    PaintHands = "PaintHands",
    PlayAnimation = "PlayAnimation",
    PlayExpression = "PlayExpression",
    PlaySong = "PlaySong",
    PlaySound = "PlaySound",
    PulseCamera = "PulseCamera",
    PulseFreeTimeBeat = "PulseFreeTimeBeat",
    ReadNarration = "ReadNarration",
    RenameWindow = "RenameWindow",
    ReorderRooms = "ReorderRooms",
    ReorderRow = "ReorderRow",
    ReorderSprite = "ReorderSprite",
    ReorderWindows = "ReorderWindows",
    SayReadyGetSetGo = "SayReadyGetSetGo",
    SetBackgroundColor = "SetBackgroundColor",
    SetBeatSound = "SetBeatSound",
    SetBeatsPerMinute = "SetBeatsPerMinute",
    SetClapSounds = "SetClapSounds",
    SetCountingSound = "SetCountingSound",
    SetCrotchetsPerBar = "SetCrotchetsPerBar",
    SetForeground = "SetForeground",
    SetGameSound = "SetGameSound",
    SetHandOwner = "SetHandOwner",
    SetHeartExplodeInterval = "SetHeartExplodeInterval",
    SetHeartExplodeVolume = "SetHeartExplodeVolume",
    SetMainWindow = "SetMainWindow",
    SetOneshotWave = "SetOneshotWave",
    SetPlayStyle = "SetPlayStyle",
    SetRoomContentMode = "SetRoomContentMode",
    SetRoomPerspective = "SetRoomPerspective",
    SetRowXs = "SetRowXs",
    SetSpeed = "SetSpeed",
    SetTheme = "SetTheme",
    SetVFXPreset = "SetVFXPreset",
    SetVisible = "SetVisible",
    SetWindowContent = "SetWindowContent",
    ShakeScreen = "ShakeScreen",
    ShakeScreenCustom = "ShakeScreenCustom",
    ShowDialogue = "ShowDialogue",
    ShowHands = "ShowHands",
    ShowRooms = "ShowRooms",
    ShowStatusSign = "ShowStatusSign",
    ShowSubdivisionsRows = "ShowSubdivisionsRows",
    SpinningRows = "SpinningRows",
    Stutter = "Stutter",
    TagAction = "TagAction",
    TextExplosion = "TextExplosion",
    Tile = "Tile",
    Tint = "Tint",
    TintRows = "TintRows",
    Unknown = "Unknown",
    WindowResize = "WindowResize",
}
const hiddenEventType = [
    EventType.ForwardDecorationEvent,
    EventType.ForwardEvent,
    EventType.ForwardRowEvent,
    EventType.MacroEvent,
    EventType.ShowSubdivisionsRows,
];
enum EventAttriblte {
    None = 0,
    DurationEvent = 0b001,
    RoomEvent = 0b010,
}
class EventInfo {
    public readonly defaultTab: Tab;
    public readonly durationKey: string = "duration";
    private readonly attr: EventAttriblte;
    public get isDurationEvent(): boolean {
        return (this.attr & EventAttriblte.DurationEvent) !== 0;
    }
    public get isRoomEvent(): boolean {
        return (this.attr & EventAttriblte.RoomEvent) !== 0;
    }
    constructor(tab: Tab);
    constructor(tab: Tab, enumAttriblte: EventAttriblte);
    constructor(tab: Tab, enumAttriblte: EventAttriblte, durationKey: string);
    constructor(
        tab: Tab,
        enumAttriblte?: EventAttriblte,
        durationKey?: string,
    ) {
        this.defaultTab = tab;
        this.attr = enumAttriblte ?? EventAttriblte.None;
        this.durationKey = durationKey ?? "duration";
    }
}
class WordInfo {
    public readonly phrase: string;
    public readonly length: number;
    constructor(phrase: string, length: number) {
        this.phrase = phrase;
        this.length = length;
    }
}
const WordInfos: Record<SayReadyGetSetGoWords, WordInfo> = {
    SayReaDyGetSetGoNew: new WordInfo("Rea, Dy, Get, Set, Go!", 4),
    SayReaDyGetSetOne: new WordInfo("Rea, Dy, Get, Set, One!", 4),
    SayGetSetGo: new WordInfo("Get, Set, Go!", 2),
    SayGetSetOne: new WordInfo("Get, Set, One!", 2),
    JustSayRea: new WordInfo("Rea", 0),
    JustSayDy: new WordInfo("Dy", 0),
    JustSayGet: new WordInfo("Get", 0),
    JustSaySet: new WordInfo("Set", 0),
    JustSayAnd: new WordInfo("And", 0),
    JustSayGo: new WordInfo("Go!", 0),
    JustSayStop: new WordInfo("Stop", 0),
    JustSayAndStop: new WordInfo("And Stop!", 0),
    SaySwitch: new WordInfo("Switch", 0),
    SayWatch: new WordInfo("Watch", 0),
    SayListen: new WordInfo("Listen", 0),
    Count1: new WordInfo("1", 0),
    Count2: new WordInfo("2", 0),
    Count3: new WordInfo("3", 0),
    Count4: new WordInfo("4", 0),
    Count5: new WordInfo("5", 0),
    Count6: new WordInfo("6", 0),
    Count7: new WordInfo("7", 0),
    Count8: new WordInfo("8", 0),
    Count9: new WordInfo("9", 0),
    Count10: new WordInfo("10", 0),
    SayReadyGetSetGo: new WordInfo("Ready, Get, Set, Go!", 4),
    JustSayReady: new WordInfo("Ready", 0),
};
const EventInfos: Record<EventType, EventInfo> = {
    AddClassicBeat: new EventInfo(Tab.Rows),
    AddFreeTimeBeat: new EventInfo(Tab.Rows),
    AddOneshotBeat: new EventInfo(Tab.Rows),
    AdvanceText: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    BassDrop: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    Blend: new EventInfo(Tab.Decorations),
    CallCustomMethod: new EventInfo(Tab.Actions),
    ChangeCharacter: new EventInfo(Tab.Actions),
    ChangePlayersRows: new EventInfo(Tab.Actions),
    Comment: new EventInfo(Tab.Unknown),
    CustomFlash: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    DesktopColor: new EventInfo(
        Tab.Windows,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    FadeRoom: new EventInfo(
        Tab.Rooms,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    FinishLevel: new EventInfo(Tab.Actions),
    Flash: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    FlipScreen: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    FloatingText: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    ForwardDecorationEvent: new EventInfo(Tab.Decorations),
    ForwardEvent: new EventInfo(Tab.Unknown, EventAttriblte.RoomEvent),
    ForwardRowEvent: new EventInfo(Tab.Rows),
    HideRow: new EventInfo(Tab.Actions),
    HideWindow: new EventInfo(Tab.Windows),
    InvertColors: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    MacroEvent: new EventInfo(Tab.Unknown),
    MaskRoom: new EventInfo(Tab.Rooms),
    Move: new EventInfo(
        Tab.Decorations,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    MoveCamera: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    MoveRoom: new EventInfo(
        Tab.Rooms,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    MoveRow: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    NarrateRowInfo: new EventInfo(Tab.Sounds),
    NewWindowDance: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    PaintHands: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    PlayAnimation: new EventInfo(Tab.Decorations),
    PlayExpression: new EventInfo(Tab.Actions),
    PlaySong: new EventInfo(Tab.Sounds),
    PlaySound: new EventInfo(Tab.Sounds),
    PulseCamera: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    PulseFreeTimeBeat: new EventInfo(Tab.Rows),
    ReadNarration: new EventInfo(Tab.Sounds),
    RenameWindow: new EventInfo(Tab.Windows),
    ReorderRooms: new EventInfo(Tab.Rooms),
    ReorderRow: new EventInfo(Tab.Actions),
    ReorderSprite: new EventInfo(Tab.Decorations),
    ReorderWindows: new EventInfo(Tab.Windows),
    SayReadyGetSetGo: new EventInfo(Tab.Sounds, EventAttriblte.RoomEvent),
    SetBackgroundColor: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    SetBeatSound: new EventInfo(Tab.Sounds),
    SetBeatsPerMinute: new EventInfo(Tab.Sounds),
    SetClapSounds: new EventInfo(Tab.Sounds),
    SetCountingSound: new EventInfo(Tab.Sounds),
    SetCrotchetsPerBar: new EventInfo(Tab.Sounds),
    SetForeground: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    SetGameSound: new EventInfo(Tab.Sounds),
    SetHandOwner: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    SetHeartExplodeInterval: new EventInfo(Tab.Sounds),
    SetHeartExplodeVolume: new EventInfo(Tab.Sounds),
    SetMainWindow: new EventInfo(Tab.Windows),
    SetOneshotWave: new EventInfo(Tab.Rows),
    SetPlayStyle: new EventInfo(Tab.Actions),
    SetRoomContentMode: new EventInfo(Tab.Rooms),
    SetRoomPerspective: new EventInfo(
        Tab.Rooms,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    SetRowXs: new EventInfo(Tab.Rows),
    SetSpeed: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    SetTheme: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    SetVFXPreset: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    SetVisible: new EventInfo(Tab.Decorations),
    SetWindowContent: new EventInfo(
        Tab.Windows,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    ShakeScreen: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    ShakeScreenCustom: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    ShowDialogue: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    ShowHands: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    ShowRooms: new EventInfo(
        Tab.Rooms,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    ShowStatusSign: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    ShowSubdivisionsRows: new EventInfo(Tab.Actions),
    SpinningRows: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    Stutter: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    TagAction: new EventInfo(Tab.Actions),
    TextExplosion: new EventInfo(Tab.Actions, EventAttriblte.RoomEvent),
    Tile: new EventInfo(
        Tab.Decorations,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    Tint: new EventInfo(
        Tab.Decorations,
        EventAttriblte.DurationEvent,
        "duration",
    ),
    TintRows: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent | EventAttriblte.RoomEvent,
        "duration",
    ),
    Unknown: new EventInfo(Tab.Unknown),
    WindowResize: new EventInfo(
        Tab.Actions,
        EventAttriblte.DurationEvent,
        "duration",
    ),
};
class Point {
    x: number = 0;
    y: number = 0;
    public constructor();
    public constructor(x: number, y: number);
    public constructor(x?: number, y?: number) {
        if (x !== undefined && y !== undefined) {
            this.x = x;
            this.y = y;
            return;
        }
    }
    public clone(): Point {
        return new Point(this.x, this.y);
    }
}
class Size {
    width: number = 0;
    height: number = 0;
    public constructor();
    public constructor(width: number, height: number);
    public constructor(width?: number, height?: number) {
        if (width !== undefined && height !== undefined) {
            this.width = width;
            this.height = height;
            return;
        }
    }
    public clone(): Point {
        return new Point(this.width, this.height);
    }
}
class Rect {
    left: number = 0;
    top: number = 0;
    right: number = 0;
    bottom: number = 0;
    public get x() {
        return this.left;
    }
    public set x(value: number) {
        const diff = value - this.left;
        this.left += diff;
        this.right += diff;
    }
    public get y() {
        return this.top;
    }
    public set y(value: number) {
        const diff = value - this.top;
        this.top += diff;
        this.bottom += diff;
    }
    public get width() {
        return this.right - this.left;
    }
    public set width(value: number) {
        this.right = this.left + value;
    }
    public get height() {
        return this.bottom - this.top;
    }
    public set height(value: number) {
        this.bottom = this.top + value;
    }
    public get centerX() {
        return (this.left + this.right) / 2;
    }
    public get centerY() {
        return (this.top + this.bottom) / 2;
    }
    public get location() {
        let point = new Point();
        point.x = this.centerX;
        point.y = this.centerY;
        return point;
    }
    public set location(point: Point) {
        let w = this.width;
        let h = this.height;
        this.left = point.x;
        this.right = this.left + w;
        this.top = point.y;
        this.bottom = this.top + h;
    }
    public get size() {
        let size = new Size();
        size.width = this.width;
        size.height = this.height;
        return size;
    }
    public set size(size: Size) {
        this.width = size.width;
        this.height = size.height;
    }
    public get isEmpty() {
        return this.width <= 0 || this.height <= 0;
    }
    public constructor();
    public constructor(size: Size);
    public constructor(point: Point, size: Size);
    public constructor(width: number, height: number);
    public constructor(x: number, y: number, width: number, height: number);
    public constructor(
        arg0?: number | Point | Size,
        arg1?: number | Size,
        arg2?: number,
        arg3?: number,
    ) {
        if (typeof arg0 === "number" && typeof arg1 === "number") {
            if (typeof arg2 === "number" && typeof arg3 === "number") {
                this.left = arg0;
                this.top = arg1;
                this.right = arg0 + arg2;
                this.bottom = arg1 + arg3;
                return;
            }
            this.right = arg0;
            this.bottom = arg1;
            return;
        } else if (arg0 instanceof Point && arg1 instanceof Size) {
            this.left = arg0.x;
            this.top = arg0.y;
            this.right = arg0.x + arg1.width;
            this.bottom = arg0.y + arg1.height;
            return;
        } else if (arg0 instanceof Size) {
            this.right = arg0.width;
            this.bottom = arg0.height;
            return;
        }
    }
    public inflate(dx: number, dy: number): void {
        this.left -= dx;
        this.right += dx;
        this.top -= dy;
        this.bottom += dy;
    }
    public clone(): Rect {
        return new Rect(this.left, this.top, this.width, this.height);
    }
    public union(rect: Rect): Rect {
        const left = Math.min(this.left, rect.left);
        const top = Math.min(this.top, rect.top);
        const right = Math.max(this.right, rect.right);
        const bottom = Math.max(this.bottom, rect.bottom);
        return new Rect(left, top, right - left, bottom - top);
    }
}
class Color {
    private argb: number = 0;
    constructor();
    constructor(hex: string);
    constructor(argb: number);
    constructor(a: number, r: number, g: number, b: number);
    constructor(a?: number | string, r?: number, g?: number, b?: number) {
        // Hex constructor supports: RGB (3), ARGB (4), RRGGBB (6), AARRGGBB (8)
        if (typeof a === "string") {
            let s = a;
            if (s.startsWith("#")) {
                s = s.substring(1);
            }
            s = s.trim();
            switch (s.length) {
                case 3: // RGB -> AARRGGBB (alpha=ff)
                    s = "ff" + s.split("").map((c) => c + c).join("");
                    break;
                case 4: // ARGB -> AARRGGBB
                    s = s.split("").map((c) => c + c).join("");
                    break;
                case 6: // RRGGBB -> AARRGGBB (alpha=ff)
                    s = "ff" + s;
                    break;
                case 8: // AARRGGBB
                    break;
                default:
                    s = "ffffffff";
            }
            const u = parseInt(s, 16);
            this.argb = u;
            return;
        }
        // Single number: treat as 0xAARRGGBB
        if (
            a !== undefined && r === undefined && g === undefined &&
            b === undefined
        ) {
            this.argb = a as number;
            return;
        }
        // RGB with implicit alpha=255
        if (
            a === undefined && r !== undefined && g !== undefined &&
            b !== undefined
        ) {
            const u = (0xFF << 24) |
                ((r & 0xFF) << 16) |
                ((g & 0xFF) << 8) |
                (b & 0xFF);
            this.argb = u;
            return;
        }
        // ARGB explicit
        if (
            a !== undefined && r !== undefined && g !== undefined &&
            b !== undefined
        ) {
            const u = ((a as number & 0xFF) << 24) |
                ((r & 0xFF) << 16) |
                ((g & 0xFF) << 8) |
                (b & 0xFF);
            this.argb = u;
            return;
        }
        this.argb = 0;
    }
    public get a(): number {
        return (this.argb >>> 24) & 0xFF;
    }
    public set a(value: number) {
        const v = value & 0xFF;
        this.argb = (v << 24) | (this.argb & 0x00FFFFFF);
    }
    public get r(): number {
        return (this.argb >>> 16) & 0xFF;
    }
    public set r(value: number) {
        const v = value & 0xFF;
        this.argb = (this.argb & 0xFF00FFFF) | (v << 16);
    }
    public get g(): number {
        return (this.argb >>> 8) & 0xFF;
    }
    public set g(value: number) {
        const v = value & 0xFF;
        this.argb = (this.argb & 0xFFFF00FF) | (v << 8);
    }
    public get b(): number {
        return this.argb & 0xFF;
    }
    public set b(value: number) {
        const v = value & 0xFF;
        this.argb = (this.argb & 0xFFFFFF00) | v;
    }
    public toUint32(): number {
        return this.argb;
    }
    public clone(): Color {
        return new Color(this.argb);
    }
    public static FromRgba(hex: string): Color;
    public static FromRgba(r: number, g: number, b: number, a: number): Color;
    public static FromRgba(
        r: number | string,
        g?: number,
        b?: number,
        a?: number,
    ): Color {
        if (
            typeof r === "string" && g === undefined && b === undefined &&
            a === undefined
        ) {
            r = r.trim();
            if (r[0] === "#") r = r.slice(1);
            if (r.length === 8) {
                return new Color(r.slice(6, 8) + r.slice(0, 6));
            } else if (r.length === 6) return new Color(r);
            else if (r.length === 4) {
                return new Color(
                    r[3] + r[3] +
                        r.slice(0, 3).split("").map((i) => i + i).join(""),
                );
            } else if (r.length === 3) {
                return new Color("FF" + r.split("").map((i) => i + i).join(""));
            } else return new Color();
        } else if (
            typeof r === "number" && typeof g === "number" &&
            typeof b === "number" && typeof a === "number"
        ) {
            return new Color(a, r, g, b);
        } else {
            return new Color();
        }
    }
    public toRgbaString(): string {
        return `rgba(${this.r}, ${this.g}, ${this.b}, ${this.a / 255})`;
    }
    public toHexString(includeAlpha: boolean = true): string {
        if (includeAlpha) {
            return `#${this.argb.toString(16).padStart(8, "0")}`;
        } else {
            const rgb = this.argb & 0x00FFFFFF;
            return `#${rgb.toString(16).padStart(6, "0")}`;
        }
    }
    public equals(other: Color): boolean {
        return this.argb === other.argb;
    }
    public static get Transparent(): Color {
        return new Color(0, 0, 0, 0);
    }
    public static get Black(): Color {
        return new Color(255, 0, 0, 0);
    }
    public static get White(): Color {
        return new Color(255, 255, 255, 255);
    }
    public withAlpha(alpha: number): Color {
        return new Color(alpha, this.r, this.g, this.b);
    }
    public withRed(red: number): Color {
        return new Color(this.a, red, this.g, this.b);
    }
    public withGreen(green: number): Color {
        return new Color(this.a, this.r, green, this.b);
    }
    public withBlue(blue: number): Color {
        return new Color(this.a, this.r, this.g, blue);
    }
}
class SliceInfo {
    public bounds: Rect = new Rect();
    public center: Rect = new Rect();
    public pivot: Point = new Point();
    public scale: number = 1;
    public get isNinePatch() {
        return !this.center.isEmpty;
    }
}
export {
    Color,
    EventAttriblte,
    EventInfos,
    EventType,
    hiddenEventType,
    Point,
    Rect,
    SayReadyGetSetGoWords,
    Size,
    SliceInfo,
    Tab,
    WordInfos,
};
