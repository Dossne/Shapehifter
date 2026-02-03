# где я и что изменилось
git status
git diff

# добавить/убрать в индекс (staging)
git add -A
git add <file_or_folder>
git restore --staged <file_or_folder>

# коммиты
git commit -m "message"
git log --oneline --max-count=20

# ветки
git branch
git switch -c feature/name
git switch main

# удалённый репозиторий
git remote -v
git push -u origin main
git push
git pull

# если надо забрать изменения без merge сразу
git fetch

# отмены (осторожно)
git reset            # снять staging, оставить файлы
git reset --hard     # снести локальные изменения

# игнор
git add .gitignore
