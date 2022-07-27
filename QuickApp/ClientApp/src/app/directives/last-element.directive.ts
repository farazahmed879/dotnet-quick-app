




import { Directive, Input, Output, EventEmitter } from '@angular/core';


@Directive({
  selector: '[appLastElement]'
})
export class LastElementDirective {
  @Input()
  set lastElement(isLastElement: boolean) {

    if (isLastElement) {
      setTimeout(() => {
        this.lastFunction.emit();
      });
    }
  }

  @Output()
  lastFunction = new EventEmitter();
}
