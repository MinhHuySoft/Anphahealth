import { Editor } from '@tiptap/core';
import { Image } from '@tiptap/extension-image';
import { TableKit } from '@tiptap/extension-table';
import { Placeholder } from '@tiptap/extensions';
import { StarterKit } from '@tiptap/starter-kit';
import DOMPurify from 'dompurify';

const instances = new WeakMap();
const emptyHtml = '<p></p>';
const headings = [2, 3, 4];
const wordCountFormat = new Intl.NumberFormat('vi-VN');

function sanitize(html) {
    return DOMPurify.sanitize(html, {
        USE_PROFILES: { html: true },
        FORBID_TAGS: ['form', 'input', 'textarea', 'button', 'select', 'style', 'iframe', 'object', 'embed'],
        FORBID_ATTR: ['style', 'srcset'],
    });
}

function allowedUrl(value, isImage = false) {
    try {
        const url = new URL(value, document.baseURI);
        const protocols = isImage ? ['http:', 'https:'] : ['http:', 'https:', 'mailto:', 'tel:'];
        return protocols.includes(url.protocol);
    } catch {
        return false;
    }
}

function normalizeUrl(value) {
    const trimmed = value.trim();
    return /^(?:www\.)[\w.-]+/i.test(trimmed) ? `https://${trimmed}` : trimmed;
}

function getInstance(root) {
    const instance = instances.get(root);
    if (!instance) {
        throw new Error('CMS editor is not ready');
    }

    return instance;
}

export function mount(root, initialHtml, label, placeholder) {
    if (instances.has(root) || !root.isConnected) {
        return;
    }

    const initialContent = sanitize(initialHtml);
    const host = root.querySelector('[data-editor-content]');
    const format = root.querySelector('[data-block-format]');
    const tableTools = root.querySelector('[data-table-tools]');
    const wordCount = root.querySelector('[data-editor-word-count]');
    const dialog = root.querySelector('[data-editor-dialog]');
    const dialogTitle = dialog.querySelector('[data-dialog-title]');
    const urlInput = dialog.querySelector('[data-dialog-url]');
    const urlLabel = dialog.querySelector('[data-url-label]');
    const detailInput = dialog.querySelector('[data-dialog-detail]');
    const detailLabel = dialog.querySelector('[data-detail-label]');
    const detailField = dialog.querySelector('[data-detail-field]');
    const dialogError = dialog.querySelector('[data-dialog-error]');
    const removeLink = dialog.querySelector('[data-dialog-remove]');
    const confirmDialog = dialog.querySelector('[data-dialog-confirm]');
    const controls = new Map([...root.querySelectorAll('[data-command]')].map(button => [button.dataset.command, button]));
    const lifecycle = new AbortController();
    const listen = (target, event, handler) => target.addEventListener(event, handler, { signal: lifecycle.signal });

    const editor = new Editor({
        element: host,
        content: initialContent,
        extensions: [
            StarterKit.configure({
                heading: { levels: [1, ...headings, 5, 6] },
                link: {
                    openOnClick: false,
                    defaultProtocol: 'https',
                    isAllowedUri: value => allowedUrl(value),
                },
            }),
            Image.configure({ inline: true }),
            TableKit,
            Placeholder.configure({ placeholder }),
        ],
        editorProps: {
            attributes: {
                class: 'cms-tiptap-prose',
                'aria-label': label,
                'aria-multiline': 'true',
                role: 'textbox',
            },
            transformPastedHTML: sanitize,
        },
    });

    const originalRenderedHtml = editor.getHTML();
    instances.set(root, { editor, initialHtml, originalRenderedHtml });
    root.dataset.ready = 'true';

    let mode = 'link';
    let selection = null;
    let dialogTrigger = null;

    function refreshToolbar() {
        const level = headings.find(level => editor.isActive('heading', { level }));
        format.value = level ? `heading${level}` : 'paragraph';

        for (const name of ['bold', 'italic', 'underline', 'strike', 'bulletList', 'orderedList', 'blockquote', 'link']) {
            controls.get(name).setAttribute('aria-pressed', editor.isActive(name));
        }

        controls.get('undo').disabled = !editor.can().undo();
        controls.get('redo').disabled = !editor.can().redo();
        controls.get('table').disabled = editor.isActive('table');
        tableTools.hidden = !editor.isActive('table');
        const words = editor.getText().trim().match(/\S+/g)?.length ?? 0;
        wordCount.textContent = `${wordCountFormat.format(words)} từ`;
    }

    function openDialog(command) {
        mode = command;
        selection = { from: editor.state.selection.from, to: editor.state.selection.to };
        dialogTrigger = controls.get(command);
        const image = mode === 'image';
        const link = image ? '' : (editor.getAttributes('link').href ?? '');
        dialogTitle.textContent = image ? 'Chèn ảnh' : 'Liên kết';
        urlLabel.textContent = image ? 'Đường dẫn ảnh' : 'Địa chỉ liên kết';
        urlInput.value = link;
        urlInput.placeholder = image ? '/media/anh-san-pham.jpg' : 'https://example.com';
        detailField.hidden = !image && (selection.from !== selection.to || !!link);
        detailLabel.textContent = image ? 'Mô tả ảnh' : 'Văn bản hiển thị';
        detailInput.value = '';
        detailInput.placeholder = image ? 'Mô tả ngắn gọn cho người đọc' : 'Tên liên kết (tùy chọn)';
        confirmDialog.textContent = image ? 'Chèn ảnh' : 'Lưu liên kết';
        removeLink.hidden = !link;
        dialogError.hidden = true;
        urlInput.removeAttribute('aria-invalid');
        dialog.showModal();
        urlInput.focus();
    }

    function submitDialog() {
        const value = normalizeUrl(urlInput.value);
        const image = mode === 'image';
        if (!value || !allowedUrl(value, image)) {
            dialogError.textContent = image ? 'Nhập đường dẫn ảnh hợp lệ.' : 'Nhập địa chỉ liên kết hợp lệ.';
            dialogError.hidden = false;
            urlInput.setAttribute('aria-invalid', 'true');
            urlInput.focus();
            return;
        }

        const detail = detailInput.value.trim();
        const chain = editor.chain().setTextSelection(selection);
        if (image) {
            chain.setImage({ src: value, alt: detail }).run();
        } else if (selection.from === selection.to && !editor.isActive('link')) {
            chain.insertContent({
                type: 'text',
                text: detail || value,
                marks: [{ type: 'link', attrs: { href: value } }],
            }).run();
        } else {
            chain.extendMarkRange('link').setLink({ href: value }).run();
        }

        dialog.close('applied');
    }

    const commands = {
        bold: chain => chain.toggleBold(),
        italic: chain => chain.toggleItalic(),
        underline: chain => chain.toggleUnderline(),
        strike: chain => chain.toggleStrike(),
        bulletList: chain => chain.toggleBulletList(),
        orderedList: chain => chain.toggleOrderedList(),
        blockquote: chain => chain.toggleBlockquote(),
        table: chain => chain.insertTable({ rows: 3, cols: 3, withHeaderRow: true }),
        addRow: chain => chain.addRowAfter(),
        addColumn: chain => chain.addColumnAfter(),
        deleteRow: chain => chain.deleteRow(),
        deleteColumn: chain => chain.deleteColumn(),
        deleteTable: chain => chain.deleteTable(),
        undo: chain => chain.undo(),
        redo: chain => chain.redo(),
    };

    listen(root, 'mousedown', event => {
        if (event.target.closest('button[data-command]')) {
            event.preventDefault();
        }
    });
    listen(root, 'click', event => {
        const action = event.target.closest('button[data-command]')?.dataset.command;
        if (action === 'link' || action === 'image') {
            openDialog(action);
        } else if (action && commands[action]) {
            commands[action](editor.chain().focus()).run();
        }
    });
    listen(format, 'change', () => {
        const choice = format.value;
        const chain = editor.chain().focus();
        if (choice === 'paragraph') {
            chain.setParagraph().run();
        } else {
            chain.setHeading({ level: Number(choice.slice('heading'.length)) }).run();
        }
    });
    listen(confirmDialog, 'click', submitDialog);
    listen(dialog.querySelector('[data-dialog-cancel]'), 'click', () => dialog.close('cancel'));
    listen(removeLink, 'click', () => {
        editor.chain().setTextSelection(selection).extendMarkRange('link').unsetLink().run();
        dialog.close('applied');
    });
    listen(dialog, 'keydown', event => {
        if (event.key === 'Enter' && !event.isComposing && event.target instanceof HTMLInputElement) {
            event.preventDefault();
            submitDialog();
        }
    });
    listen(dialog, 'close', () => {
        if (dialog.returnValue === 'applied') {
            editor.commands.focus();
        } else {
            dialogTrigger?.focus();
        }
        dialog.returnValue = '';
    });
    editor.on('transaction', refreshToolbar);
    refreshToolbar();

    // Blazor can remove the host before server-side disposal finishes.
    const observer = new MutationObserver(() => {
        if (!root.isConnected) {
            observer.disconnect();
            lifecycle.abort();
            editor.destroy();
            instances.delete(root);
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });
}

export function getHtml(root) {
    const { editor, initialHtml, originalRenderedHtml } = getInstance(root);
    const currentHtml = editor.getHTML();
    // A metadata-only save must retain the exact imported body.
    if (currentHtml === originalRenderedHtml) {
        return initialHtml;
    }

    return currentHtml === emptyHtml ? '' : sanitize(currentHtml);
}

export function getHtmlStream(root) {
    // Returning a stream keeps article bodies out of the default SignalR message size limit.
    const html = getHtml(root);
    // Blazor requires a non-empty stream. StreamReader discards the UTF-8 BOM.
    return new Blob([html || '\uFEFF'], { type: 'text/html;charset=utf-8' });
}

export function insertImage(root, source, alt = '') {
    const value = normalizeUrl(source);
    if (!value || !allowedUrl(value, true)) {
        throw new Error('Invalid CMS image URL');
    }

    getInstance(root).editor.chain().focus().setImage({ src: value, alt }).run();
}
