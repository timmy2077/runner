// Supply image MIME types omitted by the bundled multipart uploader.
const NativeBlob = globalThis.Blob;
globalThis.Blob = class extends NativeBlob {
  constructor(parts = [], options = {}) {
    if (!options.type && ArrayBuffer.isView(parts[0])) {
      const bytes = new Uint8Array(parts[0].buffer, parts[0].byteOffset, parts[0].byteLength);
      const type = bytes[0] === 0xff && bytes[1] === 0xd8 ? 'image/jpeg'
        : bytes[0] === 0x89 && bytes[1] === 0x50 ? 'image/png' : '';
      if (type) options = { ...options, type };
    }
    super(parts, options);
  }
};
