import { Point, Rect, Size, SliceInfo } from "./definition";

const assetFilePath: string = "assets.png";
const slicesFilePath: string = "assets";
const directoryPath: string = "./Assets";
const assetSize = new Size(210, 160);
const assetUrl = directoryPath + "/" + assetFilePath;
async function readFromStream(
    stream: DataView,
): Promise<Map<string, SliceInfo>> {
    let offset = 0;
    const count = stream.getUint8(0);
    offset += 1;
    const sliceInfos = new Map<string, SliceInfo>();
    for (let i = 0; i < count; i++) {
        const sliceInfo = new SliceInfo();
        const keyLength = stream.getUint8(offset);
        offset += 1;
        const key = new TextDecoder().decode(
            new Uint8Array(
                stream.buffer,
                offset,
                keyLength,
            ),
        );
        offset += keyLength;
        const left = stream.getUint8(offset);
        offset += 1;
        const top = stream.getUint8(offset);
        offset += 1;
        const wh = stream.getUint16(offset, true);
        offset += 2;
        const width = wh & 0x1F;
        const height = (wh >> 5) & 0x3F;
        sliceInfo.bounds = new Rect(left, top, width, height);
        const hasCenter = (wh & 0x8000) !== 0;
        const hasPivot = (wh & 0x4000) !== 0;
        if (hasCenter) {
            const centerByte = stream.getUint8(offset);
            offset += 1;
            const centerLeft = centerByte & 0x3;
            const centerTop = (centerByte >> 2) & 0x3;
            const centerWidth = (centerByte >> 4) & 0x3;
            const centerHeight = (centerByte >> 6) & 0x3;
            sliceInfo.center = new Rect(
                centerLeft,
                centerTop,
                centerWidth,
                centerHeight,
            );
        }
        if (hasPivot) {
            const pivotByte = stream.getUint8(offset);
            offset += 1;
            let pivotLeft = pivotByte & 0x7;
            let pivotTop = (pivotByte >> 3) & 0x7;
            if ((pivotByte & 0x80) !== 0) {
                pivotLeft *= -1;
            }
            if ((pivotByte & 0x40) !== 0) {
                pivotTop *= -1;
            }
            sliceInfo.pivot = new Point(pivotLeft, pivotTop);
        }
        sliceInfos.set(key, sliceInfo);
    }
    return sliceInfos;
}
let assetLoaded = false;
const slices = await readFromStream(
    new DataView(
        await (
            await fetch(
                directoryPath + "/" +
                    slicesFilePath,
            )
        ).arrayBuffer(),
    ),
).finally(() => {
    assetLoaded = true;
});
export { assetLoaded, assetSize, assetUrl, Point, Rect, Size, SliceInfo, slices };
