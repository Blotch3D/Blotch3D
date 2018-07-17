pandoc Blotch3DUserManual.docx -o ../readme.md
pandoc Blotch3DUserManual.docx -o Blotch3DUserManual.pdf
mkdir latex
copy ..\readme.md latex\readme.md
copy Blotch3D.doxy latex\Blotch3D.doxy
cd latex
doxygen Blotch3D.doxy
pdflatex refman.tex
pdflatex refman.tex
cd ..
copy latex\refman.pdf Blotch3D_Reference_Manual.pdf
pause