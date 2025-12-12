// Browser-only version: remove Node.js-specific imports

export class Point {
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
export class Rect {
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
    public get isEmpty() {
        return this.width <= 0 || this.height <= 0;
    }
    public constructor();
    public constructor(width: number, height: number);
    public constructor(x: number, y: number, width: number, height: number);
    public constructor(
        arg0?: number,
        arg1?: number,
        arg2?: number,
        arg3?: number,
    ) {
        if (arg0 !== undefined && arg1 !== undefined) {
            if (arg2 !== undefined && arg3 !== undefined) {
                this.left = arg0;
                this.top = arg1;
                this.right = arg0 + arg2;
                this.bottom = arg1 + arg3;
                return;
            }
            this.right = arg0;
            this.bottom = arg1;
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
export class Color {
    public red: number;
    public green: number;
    public blue: number;
    public alpha: number;
    constructor();
    constructor(hex: string);
    constructor(uint32: number);
    constructor(r: number, g: number, b: number);
    constructor(r: number, g: number, b: number, a: number);
    constructor(r?: number|string, g?: number, b?: number, a?: number) {
        if(typeof r === "string"){
            if(r.startsWith("#")){
                r = r.substring(1);
            }
            r = r.trim();
            switch(r.length){
                case 3:
                    r = r.split("").map(c => c + c).join("");
                    break;
                    case 4:
                    r = "ff" + r.split("").map(c => c + c).join("");
                    break;
                    case 6:
                    r = "ff" + r;
                    break;
                    case 8:
                    break;
                default:
                    r="ffffffff";
            }
            const u = parseInt(r, 16);
            this.alpha = (u >> 24) & 0xFF;
            this.red = (u >> 16) & 0xFF;
            this.green = (u >> 8) & 0xFF;
            this.blue = u & 0xFF;
            return;
        }
        if (
            r !== undefined && g === undefined && b === undefined &&
            a === undefined
        ) {
            this.alpha = (r >> 24) & 0xFF;
            this.red = (r >> 16) & 0xFF;
            this.green = (r >> 8) & 0xFF;
            this.blue = r & 0xFF;
            return;
        }
        if (
            r !== undefined && g !== undefined && b !== undefined &&
            a === undefined
        ) {
            this.red = r;
            this.green = g;
            this.blue = b;
            this.alpha = 255;
            return;
        }
        this.red = r ?? 0;
        this.green = g ?? 0;
        this.blue = b ?? 0;
        this.alpha = a ?? 0;
    }
    public toUint32(): number {
        return (
            ((this.alpha & 0xFF) << 24) |
            ((this.red & 0xFF) << 16) |
            ((this.green & 0xFF) << 8) |
            (this.blue & 0xFF)
        ) >>> 0;
    }
    public clone(): Color {
        return new Color(this.red, this.green, this.blue, this.alpha);
    }
}
export class Paint {
    public color: Color = new Color(255, 255, 255, 255);
}
export class SliceInfo {
    public bounds: Rect = new Rect();
    public center: Rect = new Rect();
    public pivot: Point = new Point();
    public scale: number = 1;
    public get isNinePatch() {
        return !this.center.isEmpty;
    }
}
export class AssetManager {
    public static readonly AssetFilePath: string = "assets.png";
    private static readonly SlicesFilePath: string = "assets.json";
    private static DirectoryPath: string = ".";
    public static assetFile: CanvasImageSource | null = null;
    public static slices: Map<string, SliceInfo> = new Map<
        string,
        SliceInfo
    >();
    private static _isLoaded: boolean = false;
    public constructor(dirpath: string) {
        AssetManager.DirectoryPath = dirpath;
        // In browser, caller should await initialize() explicitly
        AssetManager.initialize();
    }
    public static get isLoaded(): boolean {
        return this._isLoaded;
    }
    private static async loadAssetFile(): Promise<CanvasImageSource> {
        const pngPath = AssetManager.DirectoryPath + "/" + this.AssetFilePath;
        return await new Promise<HTMLImageElement>((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve(img);
            img.onerror = (e) => reject(new Error(`Failed to load asset file: ${pngPath}`));
            img.src = pngPath;
        });
    }
    private static async loadSlices(): Promise<Map<string, SliceInfo>> {
        if (this._isLoaded) {
            return new Map();
        }
        const jsonPath = AssetManager.DirectoryPath + "/" + this.SlicesFilePath;
        const response = await fetch(jsonPath);
        const json = await response.json();
        const slices = json["meta"]["slices"];
        const sliceInfos: Map<string, SliceInfo> = new Map<string, SliceInfo>();
        for (const slice of slices) {
            const name = slice["name"];
            const keys = slice["keys"];
            const sliceInfo = new SliceInfo();
            for (const key of keys) {
                const bounds = key["bounds"];
                sliceInfo.bounds.left = bounds["x"];
                sliceInfo.bounds.top = bounds["y"];
                sliceInfo.bounds.right = bounds["x"] + bounds["w"];
                sliceInfo.bounds.bottom = bounds["y"] + bounds["h"];
                const center = key["center"];
                if (center) {
                    sliceInfo.center.left = center["x"];
                    sliceInfo.center.top = center["y"];
                    sliceInfo.center.right = center["x"] + center["w"];
                    sliceInfo.center.bottom = center["y"] + center["h"];
                }
                const pivot = key["pivot"];
                if (pivot) {
                    sliceInfo.pivot.x = pivot["x"];
                    sliceInfo.pivot.y = pivot["y"];
                }
                const data = key["data"];
                if (data) {
                }
                sliceInfos.set(name, sliceInfo);
            }
        }
        return sliceInfos;
    }
    public static async initialize(): Promise<void> {
        if (this._isLoaded) {
            return;
        }
        this.assetFile = await this.loadAssetFile();
        this.slices = await this.loadSlices();
        this._isLoaded = true;
    }
}
