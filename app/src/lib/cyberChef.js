// All code in this file is licenced from the CyberChef project under the Apache2.0 licence
// https://github.com/gchq/CyberChef

/**
 * Translates an ordinal into a character.
 *
 * @param {number} o
 * @returns {char}
 *
 * @example
 * // returns 'a'
 * Utils.chr(97);
 */
function chr(o) {
  // Detect astral symbols
  // Thanks to @mathiasbynens for this solution
  // https://mathiasbynens.be/notes/javascript-unicode
  if (o > 0xffff) {
    o -= 0x10000;
    const high = String.fromCharCode(o >>> 10 & 0x3ff | 0xd800);
    o = 0xdc00 | o & 0x3ff;
    return high + String.fromCharCode(o);
  }

  return String.fromCharCode(o);
}

/**
 * Translates a character into an ordinal.
 *
 * @param {char} c
 * @returns {number}
 *
 * @example
 * // returns 97
 * Utils.ord('a');
 */
function ord(c) {
  // Detect astral symbols
  // Thanks to @mathiasbynens for this solution
  // https://mathiasbynens.be/notes/javascript-unicode
  if (c.length === 2) {
    const high = c.charCodeAt(0);
    const low = c.charCodeAt(1);
    if (high >= 0xd800 && high < 0xdc00 &&
      low >= 0xdc00 && low < 0xe000) {
      return (high - 0xd800) * 0x400 + low - 0xdc00 + 0x10000;
    }
  }

  return c.charCodeAt(0);
}


/**
 * Expand an alphabet range string into a list of the characters in that range.
 *
 * @param {string} alphStr
 * @returns {char[]}
 *
 * @example
 * // returns ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]
 * Utils.expandAlphRange("0-9");
 *
 * // returns ["a", "b", "c", "d", "0", "1", "2", "3", "+", "/"]
 * Utils.expandAlphRange("a-d0-3+/");
 *
 * // returns ["a", "b", "c", "d", "0", "-", "3"]
 * Utils.expandAlphRange("a-d0\\-3")
 */
export function expandAlphRange(alphStr) {
  const alphArr = [];

  for (let i = 0; i < alphStr.length; i++) {
    if (i < alphStr.length - 2 &&
      alphStr[i + 1] === "-" &&
      alphStr[i] !== "\\") {
      const start = ord(alphStr[i]),
        end = ord(alphStr[i + 2]);

      for (let j = start; j <= end; j++) {
        alphArr.push(chr(j));
      }
      i += 2;
    } else if (i < alphStr.length - 2 &&
      alphStr[i] === "\\" &&
      alphStr[i + 1] === "-") {
      alphArr.push("-");
      i++;
    } else {
      alphArr.push(alphStr[i]);
    }
  }
  return alphArr;
}

/**
 * Converts a charcode array to a string.
 *
 * @param {byteArray|Uint8Array} byteArray
 * @returns {string}
 *
 * @example
 * // returns "Hello"
 * Utils.byteArrayToChars([72,101,108,108,111]);
 *
 * // returns "你好"
 * Utils.byteArrayToChars([20320,22909]);
 */
export function byteArrayToChars(byteArray) {
  if (!byteArray || !byteArray.length) return "";
  let str = "";
  // Maxiumum arg length for fromCharCode is 65535, but the stack may already be fairly deep,
  // so don't get too near it.
  for (let i = 0; i < byteArray.length; i += 20000) {
    str += String.fromCharCode(...(byteArray.slice(i, i + 20000)));
  }
  return str;
}

/**
 * Attempts to convert a byte array to a UTF-8 string.
 *
 * @param {byteArray|Uint8Array} byteArray
 * @returns {string}
 *
 * @example
 * // returns "Hello"
 * Utils.byteArrayToUtf8([72,101,108,108,111]);
 *
 * // returns "你好"
 * Utils.byteArrayToUtf8([228,189,160,229,165,189]);
 */
export function byteArrayToUtf8(byteArray) {
  if (!byteArray || !byteArray.length) return "";
  if (!(byteArray instanceof Uint8Array))
    byteArray = new Uint8Array(byteArray);

  try {
    const str = new TextDecoder("utf-8", { fatal: true }).decode(byteArray);

    return str;
  } catch (err) {
    // If it fails, treat it as ANSI
    return byteArrayToChars(byteArray);
  }
}


/**
 * UnBase64's the input string using the given alphabet, returning a byte array.
 *
 * @param {string} data
 * @param {string} [alphabet="A-Za-z0-9+/="]
 * @param {string} [returnType="string"] - Either "string" or "byteArray"
 * @param {boolean} [removeNonAlphChars=true]
 * @returns {byteArray}
 *
 * @example
 * // returns "Hello"
 * fromBase64("SGVsbG8=");
 *
 * // returns [72, 101, 108, 108, 111]
 * fromBase64("SGVsbG8=", null, "byteArray");
 */
export function fromBase64(data, alphabet = "A-Za-z0-9+/=", returnType = "string", removeNonAlphChars = true, strictMode = false) {
  if (!data) {
    return returnType === "string" ? "" : [];
  }

  alphabet = alphabet || "A-Za-z0-9+/=";
  alphabet = expandAlphRange(alphabet).join("");

  // Confirm alphabet is a valid length
  if (alphabet.length !== 64 && alphabet.length !== 65) { // Allow for padding
    throw new Error(`Error: Base64 alphabet should be 64 characters long, or 65 with a padding character. Found ${alphabet.length}: ${alphabet}`);
  }

  // Remove non-alphabet characters
  if (removeNonAlphChars) {
    const re = new RegExp("[^" + alphabet.replace(/[[\]\\\-^$]/g, "\\$&") + "]", "g");
    data = data.replace(re, "");
  }

  if (strictMode) {
    // Check for incorrect lengths (even without padding)
    if (data.length % 4 === 1) {
      throw new Error(`Error: Invalid Base64 input length (${data.length}). Cannot be 4n+1, even without padding chars.`);
    }

    if (alphabet.length === 65) { // Padding character included
      const pad = alphabet.charAt(64);
      const padPos = data.indexOf(pad);
      if (padPos >= 0) {
        // Check that the padding character is only used at the end and maximum of twice
        if (padPos < data.length - 2 || data.charAt(data.length - 1) !== pad) {
          throw new Error(`Error: Base64 padding character (${pad}) not used in the correct place.`);
        }

        // Check that input is padded to the correct length
        if (data.length % 4 !== 0) {
          throw new Error("Error: Base64 not padded to a multiple of 4.");
        }
      }
    }
  }

  const output = [];
  let chr1, chr2, chr3,
    enc1, enc2, enc3, enc4,
    i = 0;

  while (i < data.length) {
    // Including `|| null` forces empty strings to null so that indexOf returns -1 instead of 0
    enc1 = alphabet.indexOf(data.charAt(i++) || null);
    enc2 = alphabet.indexOf(data.charAt(i++) || null);
    enc3 = alphabet.indexOf(data.charAt(i++) || null);
    enc4 = alphabet.indexOf(data.charAt(i++) || null);

    if (strictMode && (enc1 < 0 || enc2 < 0 || enc3 < 0 || enc4 < 0)) {
      throw new Error("Error: Base64 input contains non-alphabet char(s)");
    }

    chr1 = (enc1 << 2) | (enc2 >> 4);
    chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
    chr3 = ((enc3 & 3) << 6) | enc4;

    if (chr1 >= 0 && chr1 < 256) {
      output.push(chr1);
    }
    if (chr2 >= 0 && chr2 < 256 && enc3 !== 64) {
      output.push(chr2);
    }
    if (chr3 >= 0 && chr3 < 256 && enc4 !== 64) {
      output.push(chr3);
    }
  }

  return returnType === "string" ? byteArrayToUtf8(output) : output;
}
