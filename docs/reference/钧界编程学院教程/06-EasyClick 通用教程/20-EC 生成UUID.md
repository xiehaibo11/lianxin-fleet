<!-- 来源：https://www.laoguicom.top/doc/851/ · 抓取状态：ok -->

# EC生成UUID
>s 用于替代官方的获取设备唯一码函数失效的情况
使用建议:在调用前先检查本地是否已经生成了唯一设备码,如果未生成则调用函数自行保存生成的设备码到存储可文件存储也可以使用storage存储方式,然后读取后进行判断或者验证操作!
```js
/*
 * Copyright(c) 2025,
 *    项目名称:xxxx
 *    文件名称:createUUID.js
 *    创建时间:2025/3/7 16:10
 *    作者:laogui，QQ：1156346325
 */

// 时间戳工具模块（增强版）
let LGTimestampUtils = {
    /**
     * 时间戳转Base36（带前导零）
     * @param {string} timestampStr - 13位时间戳字符串
     * @returns {string} 8位Base36字符串
     */
    toBase36WithPadding: function (timestampStr) {
        // 输入校验
        if (!/^\d{13}$/.test(timestampStr)) {
            throw new Error('Invalid 13-digit timestamp');
        }

        // 直接使用 Number 处理（安全范围内）
        const num = parseInt(timestampStr, 10);
        return num.toString(36)
            .padStart(8, '0')
            .slice(-8)
            .toLowerCase();
    },

    /**
     * 生成高熵随机字符串
     * @param {number} length - 需要生成的字符长度
     * @returns {string} 随机字符串
     */
    generateHighEntropyRandom: function (length) {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        const randomBytes = this._getRandomBytes(length);
        let result = '';
        for (let j = 0; j < randomBytes.length; j++) {
            result += chars[randomBytes[j] % chars.length];
        }
        return result;
    },

    // 私有方法：获取随机字节
    _getRandomBytes: function (length) {
        if (typeof crypto !== 'undefined' && crypto.getRandomValues) {
            const buf = new Uint8Array(length);
            crypto.getRandomValues(buf);
            return buf;
        } else {
            const arr = [];
            for (let i = 0; i < length; i++) {
                arr.push(Math.floor(Math.random() * 256));
            }
            return arr;
        }
    },

    /**
     * 格式化为可读时间
     * @param {number} timestamp - 13位时间戳
     * @returns {string} YYYY-MM-DD HH:mm:ss.SSS
     */
    formatReadableTime: function (timestamp) {
        const date = new Date(timestamp);
        const pad = (n, len) => n.toString().padStart(len, '0');
        return `${date.getFullYear()}-${pad(date.getMonth() + 1, 2)}-${pad(date.getDate(), 2)} ` +
            `${pad(date.getHours(), 2)}:${pad(date.getMinutes(), 2)}:${pad(date.getSeconds(), 2)}.` +
            pad(date.getMilliseconds(), 3);
    }
};

// 主业务逻辑（增强版）
let LGIdGenerator = {
    /**
     * 生成唯一标识符
     * @param {string} javaTimestamp - Java生成的13位时间戳字符串
     * @returns {string} 32位ID（8位Base36时间戳 + 24位随机数）
     */
    createUniqueIdentifier: function (javaTimestamp) {
        return LGTimestampUtils.toBase36WithPadding(javaTimestamp) +
            LGTimestampUtils.generateHighEntropyRandom(24);
    },

    /**
     * 解码时间戳
     * @param {string} encodedId - 32位ID
     * @returns {number} 原始13位时间戳
     */
    decodeEmbeddedTimestamp: function (encodedId) {
        const base36Str = encodedId.substring(0, 8);
        return parseInt(base36Str, 36);
    },

    /**
     * 解码并格式化时间
     * @param {string} encodedId - 32位ID
     * @returns {string} 格式化后的时间字符串
     */
    decodeAndFormat: function (encodedId) {
        return LGTimestampUtils.formatReadableTime(
            this.decodeEmbeddedTimestamp(encodedId)
        );
    }
};

// 使用示例 生成唯一uuid
const uniqueId = LGIdGenerator.createUniqueIdentifier(Date.now().toString());
console.log("Generated ID:", uniqueId);
// 解码生成时间
console.log("Decoded Timestamp:", LGIdGenerator.decodeEmbeddedTimestamp(uniqueId));
// 解码并格式化时间
console.log("Formatted Time:", LGIdGenerator.decodeAndFormat(uniqueId));

```